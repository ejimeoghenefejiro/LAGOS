import { useState } from "react";
import { Link, useLocation } from "react-router-dom";
import { api, ApiError } from "../../api/client";
import type { TokenResponse } from "../../api/types";
import { useSession } from "../../state/session";
import { Roles } from "../../state/roles";
import { isNigerianPhone, phoneHint } from "../../utils/phone";

export default function PaperReceiptClaimPage() {
  const location = useLocation();
  const { session, setSession } = useSession();
  const [code, setCode] = useState(() => new URLSearchParams(location.hash.slice(1)).get("code") ?? "");
  const [phone, setPhone] = useState("");
  const [otp, setOtp] = useState("");
  const [otpRequested, setOtpRequested] = useState(false);
  const [changePhone, setChangePhone] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const verified = session?.role === Roles.Consumer && !changePhone;
  const validCode = /^[a-f0-9]{32}$/i.test(code.replace(/[ -]/g, "").trim());
  const ready = validCode && (verified || (isNigerianPhone(phone) && (!otpRequested || /^\d{6}$/.test(otp))));

  async function claim() {
    if (busy || !ready || message) return;
    setBusy(true); setError("");
    try {
      let token = verified ? session!.accessToken : undefined;
      if (!verified) {
        if (!otpRequested) {
          await api.post("/api/auth/consumer/request-otp", { phoneNumber: phone });
          setOtpRequested(true);
          return;
        }
        const auth = await api.post<TokenResponse>("/api/auth/consumer/verify-otp", { phoneNumber: phone, otp });
        setSession({ accessToken: auth.accessToken, role: auth.role, displayName: auth.displayName });
        setChangePhone(false);
        token = auth.accessToken;
      }
      const result = await api.post<{ message: string }>("/api/consumer/paper-receipts/claim", { code }, token);
      setMessage(result.message);
    } catch (err) {
      if (err instanceof ApiError && err.status === 401 && verified) {
        setChangePhone(true); setOtpRequested(false); setOtp("");
        setError("Your session expired. Verify your phone again to claim this receipt.");
      } else setError(err instanceof ApiError ? err.message : "Could not claim receipt. Please retry.");
    } finally { setBusy(false); }
  }
  return <section className="consumer-card paper-claim-card">
    <header className="paper-claim-header">
      <h1>Claim paper receipt</h1>
      <p>Enter the code printed on your receipt and your own phone number. Verify the one-time code to add the purchase to your wallet. The cashier does not need your phone number.</p>
    </header>
    <form className="paper-claim-form" onSubmit={e => { e.preventDefault(); void claim(); }}>
      <div className="paper-claim-field">
        <label htmlFor="paper-code">Receipt claim code</label>
        <input id="paper-code" className="consumer-input" disabled={busy} placeholder="Enter your 32-character code" aria-describedby="paper-code-hint" value={code} onChange={e => { setCode(e.target.value); setMessage(""); setError(""); }} autoCapitalize="characters" autoComplete="off" spellCheck={false} maxLength={64} />
        <p id="paper-code-hint" className="consumer-muted">Find this code below the QR code on your paper receipt.</p>
      </div>
      {verified ? <div className="paper-claim-account">
        <p>Claiming for <strong>{session!.displayName}</strong></p>
        {!message && <button type="button" disabled={busy} onClick={() => { setChangePhone(true); setOtpRequested(false); setOtp(""); setError(""); }}>Use a different phone number</button>}
      </div> : <>
        <div className="paper-claim-field">
          <label htmlFor="claim-phone">Your phone number</label>
          <input id="claim-phone" className="consumer-input" type="tel" inputMode="numeric" autoComplete="tel" maxLength={11} disabled={busy}
            placeholder="08139662026" aria-describedby="claim-phone-hint" value={phone}
            onChange={e => { setPhone(e.target.value); setOtpRequested(false); setOtp(""); setError(""); }} />
          <p id="claim-phone-hint" className="consumer-muted">{phoneHint} Use a number you own.</p>
        </div>
        {otpRequested && <div className="paper-claim-field">
          <label htmlFor="claim-otp">One-time verification code</label>
          <input id="claim-otp" className="consumer-input" inputMode="numeric" autoComplete="one-time-code" maxLength={6} disabled={busy}
            value={otp} onChange={e => setOtp(e.target.value.replace(/\D/g, ""))} placeholder="Enter the six-digit code" />
          <p className="consumer-muted">For this demo, the code is available in the API console. SMS delivery is not enabled.</p>
          <button type="button" disabled={busy} onClick={() => { setOtpRequested(false); setOtp(""); setError(""); }}>Request another code</button>
        </div>}
      </>}
      <button type="submit" className="consumer-button" disabled={busy || !ready || !!message}>
        {busy ? "Please wait…" : verified ? "Claim receipt" : otpRequested ? "Verify phone & claim receipt" : "Send verification code"}
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
