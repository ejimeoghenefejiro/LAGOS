import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../../api/client";
import PosIntegrationPanel from "./PosIntegrationPanel";
import { useSession } from "../../state/session";
import { loadDeliveryPreferences, saveDeliveryPreferences } from "../../state/receiptPreferences";
interface Settings {
  businessAddress?: string | null; reviewReason?: string | null;
  merchantId: string; businessName: string; businessType: string; lgaCode: string;
  lgrrsSystemId: string | null; status: string; phoneNumber: string; hasTaxId: boolean;
  createdAt: string; verifiedAt: string | null;
  posIntegrationEnabled: boolean; posConnectorAvailable: boolean;
}
export default function MerchantSettingsPage() {
  const { session, setSession } = useSession();
  const navigate = useNavigate();
  const [settings, setSettings] = useState<Settings | null>(null);
  const [channels, setChannels] = useState<string[]>([]);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(true);
  const [savingPos, setSavingPos] = useState(false);
  const [posMessage, setPosMessage] = useState("");
  const [posError, setPosError] = useState("");
  async function togglePos(enabled: boolean) {
    if (!session || !settings) return;
    setSavingPos(true); setPosMessage(""); setPosError("");
    try {
      const saved = await api.patch<{ posIntegrationEnabled: boolean; posConnectorAvailable: boolean }>(
        "/api/merchant/profile/settings/pos", { enabled }, session.accessToken);
      setSettings(current => current ? { ...current, ...saved } : current);
      setPosMessage(enabled ? "POS enabled. Generate a key to configure your POS." : "POS disabled and API key revoked.");
    } catch { setPosError("Could not save your POS preference. Please try again."); }
    finally { setSavingPos(false); }
  }
  useEffect(() => {
    if (!session) return;
    let active = true;
    api.get<Settings>("/api/merchant/profile/settings", session.accessToken).then(data => {
      if (!active) return;
      setSettings(data); setChannels(loadDeliveryPreferences(data.merchantId));
    }).catch(() => { if (active) setError("Could not load your business settings. Please reload to try again."); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [session]);
  function save() {
    if (!settings) return;
    setError(""); setMessage("");
    try {
      saveDeliveryPreferences(settings.merchantId, channels);
      setMessage("Delivery preferences saved for this business on this browser.");
    } catch (err) { setError(err instanceof Error ? err.message : "Could not save preferences."); }
  }
  return <div className="stack-lg">
    <div><h1>Settings</h1><p>Manage your receipt preferences and view your business registration.</p></div>
    {loading && <p role="status">Loading business settings…</p>}
    {error && <p className="error" role="alert">{error}</p>}
    {settings && <>
      <section className="step-card">
        <h2>Business profile</h2>
        <dl className="settings-details">
          <dt>Business name</dt><dd>{settings.businessName}</dd>
          <dt>Business type</dt><dd>{settings.businessType}</dd>
          <dt>LGA</dt><dd>{settings.lgaCode}</dd>
          <dt>Registered phone</dt><dd>{settings.phoneNumber}</dd>
        </dl>
        <p className="muted">Business identity and phone changes require verification. Editing these details is not available in this prototype.</p>
      </section>
      <section className="step-card"><h2>Application review</h2><p>Status: {settings.status === "Verified" ? "Approved" : settings.status}</p><p>{settings.reviewReason || "No administrator decision recorded yet."}</p><p>Address: {settings.businessAddress || "Not provided"}</p>
        {["Pending", "UnderReview"].includes(settings.status) && <form className="report-review-form" onSubmit={async e => {
          e.preventDefault(); const form = new FormData(e.currentTarget);
          try { await api.patch("/api/merchant/profile/settings/details", Object.fromEntries(form), session!.accessToken); const updated = await api.get<Settings>("/api/merchant/profile/settings", session!.accessToken); setSettings(updated); setMessage("Corrections submitted for administrator review."); }
          catch { setError("Could not save corrections. Check the fields and try again."); }
        }}>
          <label>Business name<input name="businessName" required maxLength={200} defaultValue={settings.businessName} /></label>
          <label>Business type<input name="businessType" required maxLength={100} defaultValue={settings.businessType} /></label>
          <label>LGA<input name="lgaCode" required maxLength={50} defaultValue={settings.lgaCode} /></label>
          <label>Business address<textarea name="businessAddress" required maxLength={500} defaultValue={settings.businessAddress ?? ""} /></label>
          <button className="primary">Submit details for review</button>
        </form>}
      </section>
      <section className="step-card">
        <h2>Registration</h2>
        <dl className="settings-details">
          <dt>LGRRS ID</dt><dd>{settings.lgrrsSystemId ?? "Not assigned"}</dd>
          <dt>Programme status</dt><dd>{settings.status}</dd>
          <dt>Registered</dt><dd>{new Date(settings.createdAt).toLocaleDateString()}</dd>
          <dt>LIRS Tax ID</dt><dd>{settings.hasTaxId ? "ID supplied — LIRS verification pending" : "Not linked yet"}</dd>
        </dl>
        <p className="muted">Official LIRS linking will be added later. Your LGRRS ID and receipt history will remain unchanged.</p>
      </section>
      <section className="step-card">
        <h2>Default receipt delivery</h2>
        <p>Choose the channels preselected when you issue a receipt. You can change them for each purchase.</p>
        <div className="settings-channel-options">{[["WHATSAPP", "WhatsApp"], ["SMS", "SMS"], ["IN_APP", "In-App"]].map(([value, label]) =>
          <label key={value}><input type="checkbox" checked={channels.includes(value)} onChange={() => {
            setMessage(""); setChannels(prev => prev.includes(value) ? prev.filter(c => c !== value) : [...prev, value]);
          }} />{label}</label>
        )}</div>
        <p className="muted">Preferences are saved for this business on this browser. SMS and WhatsApp delivery are simulated in the demo.</p>
        <button className="primary" onClick={save} disabled={!channels.length}>Save preferences</button>
        {message && <p role="status" className="success">{message}</p>}
      </section>
      <section className="step-card">
        <h2>Receipt source</h2>
        <p>Use manual receipt entry and your saved products or services, or opt in if your business already uses a POS.</p>
        <div className="settings-channel-options">
          <label><input type="checkbox" checked={settings.posIntegrationEnabled}
            disabled={savingPos} onChange={e => void togglePos(e.target.checked)}
            aria-describedby="pos-preference-description" />Connect my POS</label>
        </div>
        <p id="pos-preference-description" className="muted">Optional for any business that uses a point-of-sale system. This preference is saved to your business account.</p>
        {savingPos && <p role="status">Saving…</p>}
        {posError && <p role="alert" className="error">{posError}</p>}
        {posMessage && <p role="status">{posMessage}</p>}
        {settings.posIntegrationEnabled && settings.posConnectorAvailable && session && <PosIntegrationPanel token={session.accessToken} />}
      </section>
    </>}
    <section className="step-card"><h2>Account</h2><p>Sign out of this portal on this tab.</p>
      <button onClick={() => { setSession(null); navigate("/merchant/login", { replace: true }); }}>Sign out</button>
    </section>
  </div>;
}
