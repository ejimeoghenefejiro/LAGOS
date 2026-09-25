import { useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { api, ApiError } from "../../api/client";
import { useSession } from "../../state/session";
import { Roles } from "../../state/roles";

export default function PaperReceiptClaimPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const { session } = useSession();
  const [code, setCode] = useState(() => new URLSearchParams(location.hash.slice(1)).get("code") ?? "");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  async function claim() {
    if (session?.role !== Roles.Consumer) {
      navigate(`/check/login#code=${encodeURIComponent(code)}`); return;
    }
    setBusy(true); setError("");
    try {
      const result = await api.post<{ message: string }>("/api/consumer/paper-receipts/claim", { code }, session.accessToken);
      setMessage(result.message);
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) navigate(`/check/login#code=${encodeURIComponent(code)}`);
      else setError(err instanceof ApiError ? err.message : "Could not claim receipt. Please retry.");
    } finally { setBusy(false); }
  }
  return <section className="consumer-card paper-claim-card">
    <header className="paper-claim-header">
      <h1>Claim paper receipt</h1>
      <p>Enter the code printed on your receipt, or scan its QR code. Verify your phone to add the purchase to your wallet.</p>
    </header>
    <form className="paper-claim-form" onSubmit={e => { e.preventDefault(); if (!busy && code.trim() && !message) void claim(); }}>
      <div className="paper-claim-field">
        <label htmlFor="paper-code">Receipt claim code</label>
        <input id="paper-code" className="consumer-input" placeholder="Enter your 32-character code" aria-describedby="paper-code-hint" value={code} onChange={e => { setCode(e.target.value); setMessage(""); setError(""); }} autoCapitalize="characters" autoComplete="off" spellCheck={false} maxLength={64} />
        <p id="paper-code-hint" className="consumer-muted">Find this code below the QR code on your paper receipt.</p>
      </div>
    {session?.role === Roles.Consumer && <p className="paper-claim-account">Claiming for <strong>{session.displayName}</strong></p>}
    <button type="submit" className="consumer-button" disabled={busy || !code.trim() || !!message}>
      {busy ? "Claiming…" : session?.role === Roles.Consumer ? "Claim receipt" : "Verify phone to continue"}
    </button>
    {error && <p role="alert" className="consumer-error">{error}</p>}
    {message && <p role="status" className="paper-claim-success">{message}</p>}
    <Link className="paper-claim-wallet" to="/check/entries">Open my receipt wallet <span aria-hidden="true">→</span></Link>
    </form>
    <aside className="paper-claim-note">
      <strong>Keep your receipt code private</strong>
      <p>Each receipt can be claimed by one customer. Codes expire after 30 days. Reward entry depends on the purchase’s draw still being open.</p>
    </aside>
  </section>;
}
