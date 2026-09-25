# POS receipt submission

In Merchant Settings, enable **Connect my POS**, then select **Generate API key**.
Only verified businesses can generate keys. Copy the key immediately: the full key
is returned once and is not retrievable later. SQL stores its SHA-256 hash, display
prefix and expiry. Each business has one key, valid for 90 days. Replacement,
revocation and disabling POS invalidate the previous key immediately.

Store the key in the POS server's secret configuration, not a public browser app.
The key only submits receipts for the business that generated it; it does not
provide merchant dashboard or administrator access. Do not send a merchant ID.

## Request

Local demo endpoint: `POST http://localhost:5080/api/pos/receipts`

Headers:

```http
Content-Type: application/json
X-API-Key: YOUR_API_KEY
```

```json
{
  "externalSaleId": "BRANCH-01-TILL-02-SALE-0001",
  "receipt": {
    "customerName": "Customer",
    "customerPhone": "08012345678",
    "itemService": "Haircut",
    "amount": 3000,
    "deliveryChannels": ["IN_APP"]
  }
}
```

`externalSaleId` must be nonblank, at most 100 characters, and unique across the
business's tills/branches. Use consistent casing; database comparison follows
the configured SQL collation. The endpoint accepts requests up to 32 KB.

Amount is the completed sale total in naira, positive and at most 1 billion, with
up to two decimal places. Description is required, at most 200 characters.
Customer phone and name are optional for paper-only sales. Omit them or send null.
For paper receipts, send `"deliveryChannels": []` (omitting channels also works):

```json
{
  "externalSaleId": "SALE-0002",
  "receipt": {
    "itemService": "Bread",
    "amount": 1500,
    "deliveryChannels": []
  }
}
```

The POS handles printing. LGRRS records the transaction with status `RECORDED`,
without a digital delivery record. New sales without a phone return `claimCode`
and `claimUrl`. Print the code and encode the complete URL as a QR code using your
POS printer's QR support. Do not print a merchant API key. The customer opens the
link, verifies their own phone and confirms the claim. The existing receipt is
linked to their wallet; no second sale is created.
They still appear in receipt history and sales reporting.

Claim codes expire 30 days after sale creation and are stored as hashes. The
idempotent POS response is encrypted at rest so retries return the same printable
code. The URL carries the code in its fragment to keep it out of HTTP request
paths. Keep the printed receipt/code private: it is proof of possession.

`POST /api/consumer/paper-receipts/claim` accepts `{ "code": "PRINTED_CODE" }`
with the customer's verified consumer bearer token. The server uses the token's
identity, never a supplied customer ID. Same-customer retries succeed; another
customer gets 409. Unknown codes return 404, malformed codes 400, expired codes
410, and invalid/suspended receipts 409.

Reward eligibility is evaluated on claim, using the original purchase date and
an applicable draw that is still open. Self-entry and repeated-value checks apply.
Claims after the draw closes can still enter the wallet within 30 days, but get
no draw entry. Existing anonymous sales created before this feature have no claim
code; this migration does not create retroactive codes.

Set API configuration `PublicWebUrl` (or environment variable `PublicWebUrl`) to
the public frontend origin before remote use. It defaults to
`http://127.0.0.1:5173`; a phone scanning that localhost URL cannot reach your PC.

For digital delivery, supply the phone as a **string** to preserve leading zeros,
and a list of distinct `IN_APP`, `SMS` or `WHATSAPP` values. A phone is mandatory
when any digital delivery channel is requested. Do not insert dummy phone numbers.
The existing receipt creation, reward eligibility and fraud checks apply.

## Responses and retries

- **200:** receipt accepted. Returns the masked receipt reference, customer name,
  masked phone, description, amount, channels, delivery status and transaction date.
- **200** with `X-Idempotent-Replay: true`: original result for a retry of the same
  sale ID and receipt payload. No second receipt or reward entry is created.
- **400:** missing or invalid fields.
- **401:** missing, invalid, expired or revoked key.
- **403:** business no longer verified or POS submission disabled.
- **409:** existing sale ID supplied with different receipt details.
- **413:** request exceeds the size limit.

Persist the sale ID and payload in the POS before sending. On connection failure,
timeout or server error, retry with exponential backoff using exactly the same ID
and receipt details, including channel order. Do not retry 400/401/403/409 blindly.
Receipt creation and the deduplication record commit in one SQL transaction. A
per-business SQL lock serializes concurrent submissions, key changes and opt-out.
Retries remain deduplicated after key replacement.

## Key management

These routes require the merchant's normal bearer login, not the POS key:

- `GET /api/merchant/pos-key`: prefix, expiry and whether a key exists.
- `POST /api/merchant/pos-key`: generate or replace the key; returns its secret once.
- `POST /api/merchant/pos-key/revoke`: revoke the key.

## Demo scope

No inventory import is needed. A POS developer must implement this HTTP call in
the existing POS; enabling the setting alone does not connect hardware or import
sales. This endpoint records sales at server receipt time. Historical backfill,
refunds, itemized tax reporting and vendor-specific connectors are not implemented.
SMS/WhatsApp remain simulated, even though the demo response says `SENT`.

The localhost address is reachable only on the API machine. Before use by remote
businesses, deploy behind HTTPS with appropriate ingress rate limits, monitoring
and the production authentication/delivery providers. This change does not deploy
or publicly expose the API.

## Verification

Request an OTP for a synthetic verified merchant, then run:

```powershell
node LGRRS-Api/scripts/test-pos.mjs 08000000987 YOUR_OTP
```

The script creates three synthetic sales, checks paper-only capture, validation, key scope, replay,
conflicts, five simultaneous retries, rotation, revocation and opt-out. It revokes
the test keys and disables POS in cleanup. Use the merchant's own synthetic phone
so existing self-entry rules block these test receipts from reward eligibility.
