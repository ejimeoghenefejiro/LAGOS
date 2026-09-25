import { useState } from "react";
import { Link } from "react-router-dom";
import { api, ApiError } from "../../api/client";
import type { MerchantSummary } from "../../api/types";
import { IconHexagon, IconLock } from "../../components/icons";

const BUSINESS_TYPES = ["Barbershop", "Salon", "Supermarket", "Restaurant", "Pharmacy", "Fashion", "Electronics"];
const LGAS = ["Ikeja", "Alimosho", "Surulere", "Eti-Osa", "Kosofe", "Agege"];

export default function MerchantRegisterPage() {
  const [businessName, setBusinessName] = useState("");
  const [businessType, setBusinessType] = useState(BUSINESS_TYPES[0]);
  const [lga, setLga] = useState(LGAS[0]);
  const [address, setAddress] = useState("");
  const [phone, setPhone] = useState("");
  const [status, setStatus] = useState<MerchantSummary | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const verifyBusiness = async () => {
    setError(null);
    setBusy(true);
    try {
      const registered = status ?? await api.post<MerchantSummary>("/api/merchants/register", {
        businessName,
        businessType,
        lgaCode: lga.toUpperCase(),
        phoneNumber: phone, businessAddress: address
      });
      setStatus(registered);


    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not verify business. Check the details and try again.");
    } finally {
      setBusy(false);
    }
  };

  const canVerify = businessName.trim() && phone.trim() && address.trim();

  return (
    <div className="merchant-onboard-shell">
      <header className="merchant-onboard-header">
        <IconHexagon width={30} height={30} className="brand-mark" />
        <div>
          <div className="merchant-title">LGRRS Merchant Portal</div>
          <div className="merchant-subtitle">Local Government Receipt Reward Scheme</div>
        </div>
        <span className="demo-pill">Demo Data</span>
      </header>

      <main className="merchant-onboard-main">
        <section className="step-card wide">
          <div className="step-header">
            <span className="step-badge tone-blue">1</span>
            <div className="step-icon tone-blue"><IconLock width={20} height={20} /></div>
            <div>
              <h2>Register your business</h2>
              <p className="muted">Enter your business details. We will generate your unique LGRRS ID automatically. No tax ID is needed for this demo.</p>
            </div>
          </div>

          <fieldset className="form-grid registration-fields" disabled={busy || status !== null}>
            <label className="field-label">
              Business Name
              <input value={businessName} onChange={(e) => setBusinessName(e.target.value)} placeholder="Ayo Fadez Barbers" />
            </label>
            <label className="field-label">
              Business Type
              <select value={businessType} onChange={(e) => setBusinessType(e.target.value)}>
                {BUSINESS_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
              </select>
            </label>
            <label className="field-label">
              LGA
              <select value={lga} onChange={(e) => setLga(e.target.value)}>
                {LGAS.map((l) => <option key={l} value={l}>{l}</option>)}
              </select>
            </label>
            <label className="field-label">
              LGRRS System ID
              <input readOnly value={status?.lgrrsSystemId ?? ""} placeholder="Generated when you register" />
            </label>
            <label className="field-label full-span">
              Phone Number
              <input value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="0803 456 7812" />
            </label>
            <label className="field-label full-span">Business address<input value={address} onChange={e => setAddress(e.target.value)} maxLength={500} placeholder="Street address, area and city" /></label>
          </fieldset>
          <p className="muted">Your LGRRS ID identifies your business in this programme. An official LIRS Tax ID can be linked later without losing your receipts or history. Demo verification does not verify tax registration.</p>

          <div className="step-actions">
            <button className="outline tone-blue" onClick={verifyBusiness} disabled={!canVerify || busy || status !== null}>
              {busy ? "Registering..." : status ? "Submitted for review" : "Register business"}
            </button>
            {status ? (
              <Link to="/merchant/login" className="primary tone-blue">Continue →</Link>
            ) : (
              <button className="primary tone-blue" disabled>Continue →</button>
            )}
          </div>

          {error && <p className="error">{error}</p>}
          {status?.lgrrsSystemId && <p role="status"><strong>Your business ID: {status.lgrrsSystemId}</strong><br />Keep this ID for your records.</p>}
          {status && (
            <p className={status.status === "Verified" ? "success" : "muted"}>
              Status: {status.status}. {status.status === "Verified" ? "You can now sign in with your phone number and OTP." : "Sign in with your phone and OTP to view the application. Administrator approval is required before issuing receipts."}
            </p>
          )}
        </section>
      </main>
    </div>
  );
}
