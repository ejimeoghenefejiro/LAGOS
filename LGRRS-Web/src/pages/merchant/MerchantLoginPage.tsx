import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api, ApiError } from "../../api/client";
import type { TokenResponse } from "../../api/types";
import { useSession } from "../../state/session";

export default function MerchantLoginPage() {
  const [phone, setPhone] = useState("");
  const [otp, setOtp] = useState("");
  const [otpRequested, setOtpRequested] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const { setSession } = useSession();
  const navigate = useNavigate();

  const requestOtp = async () => {
    setError(null);
    try {
      await api.post(`/api/auth/merchant/request-otp`, { phoneNumber: phone });
      setOtpRequested(true);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not send OTP.");
    }
  };

  const verifyOtp = async () => {
    setError(null);
    try {
      const token = await api.post<TokenResponse>(`/api/auth/merchant/verify-otp`, { phoneNumber: phone, otp });
      setSession({ accessToken: token.accessToken, role: token.role, displayName: token.displayName });
      navigate("/merchant");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Invalid or expired OTP.");
    }
  };

  return (
    <div className="page-card">
      <h1>Merchant Access</h1>
      <p className="muted">Verified businesses sign in with a phone-number OTP.</p>

      <div className="field-column">
        <input placeholder="Business phone number" value={phone} onChange={(e) => setPhone(e.target.value)} />
        <button onClick={requestOtp} disabled={!phone.trim()}>Send OTP</button>
      </div>

      {otpRequested && (
        <div className="field-column">
          <input placeholder="Enter OTP" value={otp} onChange={(e) => setOtp(e.target.value)} />
          <button className="primary" onClick={verifyOtp} disabled={!otp.trim()}>Verify &amp; Continue</button>
        </div>
      )}

      {error && <p className="error">{error}</p>}

      <p className="muted small" style={{ marginTop: 16 }}>
        New business? <Link to="/merchant/register">Register here</Link>
      </p>
    </div>
  );
}
