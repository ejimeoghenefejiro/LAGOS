import { isNigerianPhone, phoneHint } from "../../utils/phone";
import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api, ApiError } from "../../api/client";
import type { TokenResponse } from "../../api/types";
import { useSession } from "../../state/session";

export default function MerchantLoginPage() {
  const [phone, setPhone] = useState("");
  const [otp, setOtp] = useState("");
  const [passcode, setPasscode] = useState("");
  const [confirm, setConfirm] = useState("");
  const [setup, setSetup] = useState(false);
  const [otpRequested, setOtpRequested] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { setSession } = useSession();
  const navigate = useNavigate();
  const validPasscode = /^\d{6}$/.test(passcode);

  const submit = async () => {
    setError(null);
    if (!isNigerianPhone(phone)) { setError(phoneHint); return; }
    setBusy(true);
    try {
      if (setup && !otpRequested) {
        await api.post("/api/auth/merchant/request-otp", { phoneNumber: phone });
        setOtpRequested(true);
        return;
      }
      const token = await api.post<TokenResponse>(setup ? "/api/auth/merchant/verify-otp" : "/api/auth/merchant/login",
        { phoneNumber: phone, passcode, ...(setup ? { otp } : {}) });
      setSession({ accessToken: token.accessToken, role: token.role, displayName: token.displayName });
      navigate("/merchant");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not sign in. Please try again.");
    } finally { setBusy(false); }
  };

  const changeMode = () => {
    setSetup(!setup); setOtpRequested(false); setOtp(""); setPasscode(""); setConfirm(""); setError(null);
  };

  return (
    <div className="page-card">
      <h1>{setup ? "Set up or reset your passcode" : "Merchant Access"}</h1>
      <p className="muted">{setup
        ? "Verify your business phone with a one-time code and choose your own six-digit passcode. Use it for future logins."
        : "Sign in with your business phone number and six-digit passcode."}</p>
      <form className="field-column" onSubmit={e => { e.preventDefault(); if (!busy) void submit(); }}>
        <label className="field-label">Business phone number
          <input type="tel" inputMode="numeric" maxLength={11} pattern="0[789][0-9]{9}" title={phoneHint} autoComplete="tel" required value={phone} disabled={busy || otpRequested}
            onChange={e => setPhone(e.target.value)} placeholder="08139662026" />
        </label>
        <p className="muted small">{phoneHint}</p>
        {setup && otpRequested && <label className="field-label">One-time verification code
          <input inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" maxLength={6} required value={otp}
            onChange={e => setOtp(e.target.value.replace(/\D/g, ""))} />
        </label>}
        {(!setup || otpRequested) && <label className="field-label">{setup ? "Choose a six-digit passcode" : "Passcode"}
          <input type="password" inputMode="numeric" autoComplete={setup ? "new-password" : "current-password"}
            pattern="[0-9]{6}" maxLength={6} required value={passcode}
            onChange={e => setPasscode(e.target.value.replace(/\D/g, ""))} />
        </label>}
        
        {setup && otpRequested && <label className="field-label">Confirm passcode
          <input type="password" inputMode="numeric" autoComplete="new-password" pattern="[0-9]{6}" maxLength={6} required value={confirm}
            onChange={e => setConfirm(e.target.value.replace(/\D/g, ""))} />
        </label>}
        {setup && confirm && confirm !== passcode && <p className="error">Passcodes do not match.</p>}
        <button className="primary" type="submit" disabled={busy || !isNigerianPhone(phone) || ((!setup || otpRequested) && !validPasscode) || (setup && otpRequested && (otp.length !== 6 || confirm !== passcode))}>
          {busy ? "Please wait..." : setup ? otpRequested ? "Verify phone & save passcode" : "Send verification code" : "Sign in"}
        </button>
        {setup && otpRequested && <button type="button" disabled={busy} onClick={() => { setOtpRequested(false); setOtp(""); setError(null); }}>Change phone or request another code</button>}
      </form>
      {error && <p className="error" role="alert">{error}</p>}
      <button type="button" disabled={busy} onClick={changeMode} style={{ marginTop: 16 }}>
        {setup ? "Back to passcode login" : "First time signing in or forgot passcode?"}
      </button>
      <p className="muted small" style={{ marginTop: 16 }}>New business? <Link to="/merchant/register">Register here</Link></p>
    </div>
  );
}


