import { isNigerianPhone, phoneHint } from "../../utils/phone";
import { useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { api, ApiError } from "../../api/client";
import type { TokenResponse } from "../../api/types";
import { useSession } from "../../state/session";
import { IconHexagon } from "../../components/icons";

export default function ConsumerLoginPage() {
  const [phone, setPhone] = useState("");
  const [otp, setOtp] = useState("");
  const [otpRequested, setOtpRequested] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { setSession } = useSession();
  const navigate = useNavigate();
  const location = useLocation();
  const claimCode = new URLSearchParams(location.hash.slice(1)).get("code");

  const requestOtp = async () => {
    setError(null); if (!isNigerianPhone(phone)) { setError(phoneHint); return; }
    try {
      await api.post("/api/auth/consumer/request-otp", { phoneNumber: phone });
      setOtpRequested(true);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not send OTP.");
    }
  };

  const verifyOtp = async () => {
    setError(null); if (!isNigerianPhone(phone)) { setError(phoneHint); return; }
    try {
      const token = await api.post<TokenResponse>("/api/auth/consumer/verify-otp", { phoneNumber: phone, otp });
      setSession({ accessToken: token.accessToken, role: token.role, displayName: token.displayName });
      navigate(claimCode ? `/check/claim#code=${encodeURIComponent(claimCode)}` : "/check/entries");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Invalid or expired OTP.");
    }
  };

  return (
    <div className="consumer-shell">
      <div className="consumer-phone">
        <header className="consumer-header">
          <div className="consumer-header-left">
            <IconHexagon width={34} height={34} className="consumer-logo" />
            <div>
              <div className="consumer-title">LGRRS</div>
              <div className="consumer-subtitle">Lagos LGA Receipt Reward Scheme</div>
            </div>
          </div>
        </header>

        <main className="consumer-content">
          <div className="consumer-card" style={{ marginTop: 24 }}>
            <h2 className="consumer-heading" style={{ textAlign: "center" }}>Verify Your Phone</h2>
            <p className="consumer-muted" style={{ textAlign: "center" }}>
              Verify your phone to access your wallet or claim a paper receipt.
              In this prototype, the one-time code appears in the API console.
            </p>
            {claimCode && <p>Your receipt code is ready. Verify your phone to continue claiming it.</p>}
            <p><Link to={claimCode ? `/check/claim#code=${encodeURIComponent(claimCode)}` : "/check/claim"}>Claim paper receipt</Link></p>

            <div className="consumer-field-column">
              <input className="consumer-input" type="tel" inputMode="numeric" maxLength={11} pattern="0[789][0-9]{9}" aria-label="Nigerian mobile number" placeholder="08139662026" value={phone} onChange={(e) => { setPhone(e.target.value); setOtpRequested(false); setOtp(""); }} />
              <p className="consumer-muted">{phoneHint}</p>
              <button className="consumer-button-outline" onClick={requestOtp} disabled={!isNigerianPhone(phone)}>Send OTP</button>
            </div>

            {otpRequested && (
              <div className="consumer-field-column">
                <input className="consumer-input" placeholder="Enter OTP" value={otp} onChange={(e) => setOtp(e.target.value)} />
                <button className="consumer-button" onClick={verifyOtp} disabled={!isNigerianPhone(phone) || !otp.trim()}>Verify &amp; Continue</button>
              </div>
            )}

            {error && <p className="consumer-error">{error}</p>}
          </div>
        </main>
      </div>
    </div>
  );
}

