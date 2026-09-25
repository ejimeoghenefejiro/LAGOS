import { useEffect, useState } from "react";
import { api, BASE_URL } from "../../api/client";

interface KeyStatus { hasKey: boolean; prefix: string | null; expiresAt: string | null }
export default function PosIntegrationPanel({ token }: { token: string }) {
  const [status, setStatus] = useState<KeyStatus | null>(null);
  const [key, setKey] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  useEffect(() => {
    let active = true;
    api.get<KeyStatus>("/api/merchant/pos-key", token).then(value => { if (active) setStatus(value); })
      .catch(() => { if (active) setError("Could not load API key status. Reload to try again."); });
    return () => { active = false; };
  }, [token]);
  async function changeKey(revoke = false) {
    if (status?.hasKey && !window.confirm(revoke ? "Revoke this key? Your POS will stop submitting receipts." : "Replace this key? The previous key will stop working immediately.")) return;
    setBusy(true); setError(""); setMessage("");
    try {
      if (revoke) {
        await api.post("/api/merchant/pos-key/revoke", undefined, token);
        setKey(""); setStatus({ hasKey: false, prefix: null, expiresAt: null });
        setMessage("Key revoked.");
      } else {
        const value = await api.post<{ apiKey: string; prefix: string; expiresAt: string }>("/api/merchant/pos-key", undefined, token);
        setKey(value.apiKey); setStatus({ prefix: value.prefix, expiresAt: value.expiresAt, hasKey: true });
      }
    } catch { setError("Could not update the key. Check that your business is verified and try again."); }
    finally { setBusy(false); }
  }
  return <div className="stack-lg">
    <div><h3>POS receipt API</h3><p>Connect your existing POS to submit completed sales. No inventory import is needed. Your POS provider must configure the connection.</p></div>
    {error && <p role="alert" className="error">{error}</p>}
    {status && <>
      <p>{status.hasKey ? `Key: ${status.prefix}… · Expires ${new Date(status.expiresAt!).toLocaleDateString()}` : "No API key generated."}</p>
      <div className="settings-channel-options">
        <button disabled={busy} onClick={() => void changeKey()}>{status.hasKey ? "Replace API key" : "Generate API key"}</button>
        {status.hasKey && <button disabled={busy} onClick={() => void changeKey(true)}>Revoke key</button>}
      </div>
    </>}
    {key && <div>
      <label htmlFor="pos-secret">Your API key — shown only now</label>
      <input id="pos-secret" readOnly value={key} autoComplete="off" spellCheck={false} />
      <button onClick={async () => {
        try { await navigator.clipboard.writeText(key); setMessage("API key copied."); }
        catch { setError("Could not copy. Select the key and copy it manually."); }
      }}>Copy key</button>
    </div>}
    {message && <p role="status">{message}</p>}
    <p className="muted">Keep the key in your POS server configuration. It can submit receipts only for this business. Keys expire after 90 days. Turning off POS integration revokes your key.</p>
    <div><h3>Send a completed sale</h3>
      <p style={{ overflowWrap: "anywhere" }}><strong>POST</strong> {BASE_URL}/api/pos/receipts</p>
      <p>Headers: <code>Content-Type: application/json</code> and <code>X-API-Key: YOUR_API_KEY</code></p>
      <p>For a paper receipt, no customer details are needed. Your POS prints the receipt; LGRRS records the sale.</p>
      <pre style={{ overflowX: "auto", maxWidth: "100%", padding: "1rem" }}>{JSON.stringify({ externalSaleId: "SALE-0001", receipt: { itemService: "Haircut", amount: 3000, deliveryChannels: [] } }, null, 2)}</pre>
      <p>To deliver digitally, also supply <code>customerPhone</code> as a string, such as <code>"08012345678"</code>, and choose <code>IN_APP</code>, <code>SMS</code> or <code>WHATSAPP</code>. Without a phone, the API returns claimCode and claimUrl. Print the code and encode claimUrl as a QR code on the paper receipt. Customers verify their own phone to claim it later.</p>
      <p>Use a unique sale ID for each completed sale. Retry with the same ID and details to avoid duplicate receipts. Changed details with an existing ID return a conflict.</p>
      <p className="muted">This demo uses the local API address above. Remote POS systems need a deployed HTTPS address. SMS and WhatsApp delivery are simulated.</p>
    </div>
  </div>;
}
