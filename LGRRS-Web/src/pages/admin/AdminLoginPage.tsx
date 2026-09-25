import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { api, ApiError } from "../../api/client";
import type { TokenResponse } from "../../api/types";
import { useSession } from "../../state/session";

export default function AdminLoginPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const { setSession } = useSession();
  const navigate = useNavigate();

  const login = async () => {
    setError(null);
    try {
      const token = await api.post<TokenResponse>("/api/auth/staff/login", { email, password });
      setSession({ accessToken: token.accessToken, role: token.role, displayName: token.displayName });
      navigate("/admin");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Invalid credentials.");
    }
  };

  return (
    <div className="page-card">
      <h1>LGA Administrator Sign In</h1>
      <div className="field-column">
        <input placeholder="Email" value={email} onChange={(e) => setEmail(e.target.value)} />
        <input placeholder="Password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
        <button className="primary" onClick={login} disabled={!email || !password}>Sign In</button>
      </div>
      {error && <p className="error">{error}</p>}
      {import.meta.env.DEV && <p className="muted">Demo credentials: admin@lgrrs.demo / Demo#12345</p>}
    </div>
  );
}
