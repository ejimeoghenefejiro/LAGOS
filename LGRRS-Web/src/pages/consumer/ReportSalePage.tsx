import { useEffect, useState, type FormEvent } from "react";
import { Navigate } from "react-router-dom";
import { api } from "../../api/client";
import { Roles } from "../../state/roles";
import { useSession } from "../../state/session";

interface Report { saleReportId: string; businessName: string; amount: number; status: string; }
export default function ReportSalePage() {
  const { session } = useSession();
  const [reports, setReports] = useState<Report[]>([]);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  useEffect(() => {
    if (session?.role !== Roles.Consumer) return;
    api.get<Report[]>("/api/sale-reports/mine", session.accessToken).then(setReports)
      .catch(() => setError("Could not load your reports. Please reload."));
  }, [session]);
  if (session?.role !== Roles.Consumer) return <Navigate to="/check/login" replace />;
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    setBusy(true); setError(""); setMessage("");
    try {
      const result = await api.post<{ saleReportId: string; status: string }>("/api/sale-reports", {
        businessName: data.get("businessName"), businessLocation: data.get("businessLocation"),
        lgaCode: data.get("lgaCode"), amount: Number(data.get("amount")),
        purchaseDate: new Date(String(data.get("purchaseDate"))).toISOString(),
        issue: data.get("issue"), details: data.get("details")
      }, session!.accessToken);
      setReports(prev => [{ ...result, businessName: String(data.get("businessName")), amount: Number(data.get("amount")) }, ...prev]);
      setMessage("Report received for review. It has not been counted as a verified sale or lottery entry.");
      form.reset();
    } catch (err) { setError(err instanceof Error ? err.message : "Could not submit report."); }
    finally { setBusy(false); }
  }
  return <div className="consumer-page-stack">
    <h1>Make your purchase count</h1>
    <p>No receipt, wrong amount, or an unrecognised receipt? Tell the programme what happened so an administrator can follow up.</p>
    <p className="consumer-muted">Reports are unverified leads. Reporting does not prove a violation or award a prize. Your phone number is not shown in the review queue.</p>
    <form className="stack-md consumer-card" onSubmit={submit}>
      <label className="consumer-field-label">Business name<input className="consumer-input" name="businessName" required maxLength={200} /></label>
      <label className="consumer-field-label">Business address or landmark<input className="consumer-input" name="businessLocation" required maxLength={300} /></label>
      <label className="consumer-field-label">LGA<input className="consumer-input" name="lgaCode" defaultValue="Ikeja" required maxLength={50} /></label>
      <label className="consumer-field-label">Purchase date and time<input className="consumer-input" type="datetime-local" name="purchaseDate" required /></label>
      <label className="consumer-field-label">Amount paid (₦)<input className="consumer-input" type="number" name="amount" min="0.01" max="1000000000" step="0.01" required /></label>
      <label className="consumer-field-label">What happened?<select className="consumer-input" name="issue">
        <option value="MissingReceipt">The business did not issue a receipt</option>
        <option value="IncorrectAmount">The receipt amount is incorrect</option>
        <option value="UnrecognisedReceipt">The receipt is not recognised</option>
      </select></label>
      <label className="consumer-field-label">Purchase details<textarea className="consumer-input" name="details" rows={4} maxLength={1000} required placeholder="What did you buy and what happened? Do not include bank details or other sensitive information." /></label>
      <button className="consumer-button" disabled={busy}>{busy ? "Submitting…" : "Submit for review"}</button>
    </form>
    {error && <p className="error" role="alert">{error}</p>}
    {message && <p role="status">{message}</p>}
    <h2>My reports</h2>
    {reports.map(r => <article className="consumer-card" key={r.saleReportId}><strong>{r.businessName}</strong><p>₦{r.amount.toLocaleString()} · {r.status}</p><small>Reference: {r.saleReportId}</small></article>)}
  </div>;
}
