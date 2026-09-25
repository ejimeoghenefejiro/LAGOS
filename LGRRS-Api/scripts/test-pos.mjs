// Local demo integration test. Request an OTP first, then pass phone and OTP as arguments.
// Creates three synthetic sales. Revokes keys and disables POS in finally.
import assert from 'node:assert/strict';
const [phoneNumber, otp] = process.argv.slice(2);
if (!phoneNumber || !otp) throw Error('Usage: node scripts/test-pos.mjs PHONE OTP');
const base = 'http://localhost:5080';
async function call(path, method = 'GET', body, headers = {}) {
  return fetch(base + path, { method, headers: { 'Content-Type': 'application/json', ...headers },
    body: body === undefined ? undefined : JSON.stringify(body) });
}
const auth = await call('/api/auth/merchant/verify-otp', 'POST', { phoneNumber, otp });
assert.equal(auth.status, 200, 'OTP login');
const { accessToken } = await auth.json();
const bearer = { Authorization: `Bearer ${accessToken}` };
const settings = '/api/merchant/profile/settings/pos';
const keys = '/api/merchant/pos-key';
const path = '/api/pos/receipts';
const sale = { externalSaleId: `POS-TEST-${Date.now()}`, receipt: {
  customerName: 'Synthetic POS Customer', customerPhone: phoneNumber,
  itemService: 'Synthetic POS test service', amount: 3000, deliveryChannels: ['IN_APP'] } };
try {
  assert.equal((await call(settings, 'PATCH', { enabled: false }, bearer)).status, 200);
  assert.equal((await call(keys, 'POST', undefined, bearer)).status, 409, 'Disabled business cannot generate key');
  assert.equal((await call(keys)).status, 401, 'Anonymous key management denied');
  assert.equal((await call(settings, 'PATCH', { enabled: true }, bearer)).status, 200);
  const created = await call(keys, 'POST', undefined, bearer);
  assert.equal(created.status, 200, 'Generate key');
  const { apiKey } = await created.json();
  assert.match(apiKey, /^lgrrs_pos_[a-f0-9]{64}$/);
  const keyHeader = { 'X-API-Key': apiKey };
  const status = await (await call(keys, 'GET', undefined, bearer)).json();
  assert.equal(status.hasKey, true); assert.equal(status.apiKey, undefined, 'Raw key cannot be fetched again');
  assert.equal((await call(path, 'POST', sale)).status, 401, 'Missing key denied');
  assert.equal((await call('/api/merchant/receipts', 'GET', undefined, keyHeader)).status, 401, 'Key cannot read merchant receipts');
  assert.equal((await call(path, 'POST', { ...sale, externalSaleId: ' ' }, keyHeader)).status, 400, 'Blank sale denied');
  assert.equal((await call(path, 'POST', { ...sale, receipt: { ...sale.receipt, amount: -1 } }, keyHeader)).status, 400, 'Invalid amount denied');
  const paper = { externalSaleId: sale.externalSaleId + '-paper', receipt: {
    itemService: 'Synthetic anonymous paper sale', amount: 1500, deliveryChannels: [] } };
  const paperResult = await call(path, 'POST', paper, keyHeader);
  assert.equal(paperResult.status, 200, await paperResult.clone().text());
  const recorded = await paperResult.json();
  assert.equal(recorded.deliveryStatus, 'RECORDED');
  assert.deepEqual(recorded.channels, []);
  assert.equal(recorded.customerPhoneMasked, '');
  const omitted = { externalSaleId: paper.externalSaleId, receipt: { itemService: paper.receipt.itemService, amount: 1500 } };
  const paperReplay = await call(path, 'POST', omitted, keyHeader);
  assert.equal(paperReplay.status, 200);
  assert.equal(paperReplay.headers.get('x-idempotent-replay'), 'true', 'Omitted channels replay the paper sale');
  assert.equal((await call(path, 'POST', { ...paper, receipt: { ...paper.receipt, customerPhone: null } }, keyHeader)).status, 200, 'Null phone replays paper sale');
  for (const channel of ['IN_APP', 'SMS', 'WHATSAPP']) {
    assert.equal((await call(path, 'POST', { ...paper, externalSaleId: paper.externalSaleId + channel,
      receipt: { ...paper.receipt, deliveryChannels: [channel] } }, keyHeader)).status, 400, 'Digital delivery needs phone');
  }
  assert.equal((await call('/api/merchant/receipts', 'POST', paper.receipt, bearer)).status, 400, 'Manual flow still requires phone and channel');
  const first = await call(path, 'POST', sale, keyHeader);
  assert.equal(first.status, 200, await first.clone().text());
  const firstText = await first.text();
  const replay = await call(path, 'POST', sale, keyHeader);
  assert.equal(replay.headers.get('x-idempotent-replay'), 'true');
  assert.equal(await replay.text(), firstText, 'Replay returns original receipt');
  assert.equal((await call(path, 'POST', { ...sale, receipt: { ...sale.receipt, amount: 4500 } }, keyHeader)).status, 409, 'Conflicting reuse denied');
  const concurrent = { ...sale, externalSaleId: sale.externalSaleId + '-concurrent', receipt: { ...sale.receipt, itemService: 'Concurrent synthetic POS test' } };
  const responses = await Promise.all(Array.from({ length: 5 }, () => call(path, 'POST', concurrent, keyHeader)));
  for (const r of responses) assert.equal(r.status, 200);
  assert.equal(new Set(await Promise.all(responses.map(r => r.text()))).size, 1, 'Concurrent retries return one receipt');
  assert.equal(responses.filter(r => r.headers.get('x-idempotent-replay') === 'true').length, 4);
  const replacement = await call(keys, 'POST', undefined, bearer);
  assert.equal(replacement.status, 200);
  const key2 = { 'X-API-Key': (await replacement.json()).apiKey };
  assert.equal((await call(path, 'POST', sale, keyHeader)).status, 401, 'Replaced key denied');
  assert.equal((await call(path, 'POST', sale, key2)).status, 200, 'Replay survives rotation');
  assert.equal((await call(keys + '/revoke', 'POST', undefined, bearer)).status, 204);
  assert.equal((await call(path, 'POST', sale, key2)).status, 401, 'Revoked key denied');
  const key3 = { 'X-API-Key': (await (await call(keys, 'POST', undefined, bearer)).json()).apiKey };
  assert.equal((await call(settings, 'PATCH', { enabled: false }, bearer)).status, 200);
  assert.equal((await call(settings, 'PATCH', { enabled: true }, bearer)).status, 200);
  assert.equal((await call(path, 'POST', sale, key3)).status, 401, 'Disabling permanently revokes key');
  console.log('PASS: paper sale without customer details, optional channels, digital phone validation, generation, scope, replay, conflict, concurrent retries, rotation, revocation, opt-out.');
} finally {
  await call(keys + '/revoke', 'POST', undefined, bearer);
  await call(settings, 'PATCH', { enabled: false }, bearer);
}
