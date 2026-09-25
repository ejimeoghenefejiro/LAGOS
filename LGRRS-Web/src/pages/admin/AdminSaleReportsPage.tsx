import { useEffect, useState, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { api } from "../../api/client";
import { useSession } from "../../state/session";
interface Report {
  saleReportId: string; businessName: string; businessLocation: string; lgaCode: string;
  amount: number; purchaseDate: string; issue: string; details: string; status: string; reviewNote: string;
  createdAt: string; reviewedAt?: string;
}
interface Detail { report: Report; history: { eventId: string; eventType: string; timestamp: string; metadata: string | null; actorName: string | null; actorRole: string | null }[] }
const label = (s: string) => s.replace(/([a-z])([A-Z])/g, "$1 $2");
const date = (s: string) => new Date(s).toLocaleString();
function note(value: string | null) {
  if (!value) return "";
  try { const data = JSON.parse(value); return `${data.Status ?? ""}: ${data.Note ?? ""}`; } catch { return value; }
}
export default function AdminSaleReportsPage() {
  const { session } = useSession();
  const [params] = useSearchParams();
  const selectedId = params.get("report");
  const [reports, setReports] = useState<Report[]>([]);
  const [detail, setDetail] = useState<Detail | null>(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [query, setQuery] = useState("");
  const [filter, setFilter] = useState("All");
  const [revision, setRevision] = useState(0);
  useEffect(() => {
    if (!session) return;
    let active = true;
    setLoading(true); setError(""); setDetail(null);
    const load = selectedId
      ? api.get<Detail>(`/api/sale-reports/${encodeURIComponent(selectedId)}`, session.accessToken).then(d => { if (active) setDetail(d); })
      : api.get<Report[]>("/api/sale-reports", session.accessToken).then(d => { if (active) setReports(d); });
    load.catch(() => { if (active) setError("Could not load this report view. It may be unavailable, or your session may have expired."); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [session, selectedId, revision]);
  async function review(event: FormEvent<HTMLFormElement>, report: Report) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const note = String(data.get("note")).trim();
    if (!note) { setError("Enter a review note."); return; }
    setBusy(true); setError("");
    try {
      await api.patch(`/api/sale-reports/${report.saleReportId}`, { status: String(data.get("status")), note }, session!.accessToken);
      setRevision(v => v + 1);
    } catch (err) { setError(err instanceof Error ? err.message : "Could not save review."); }
    finally { setBusy(false); }
  }
  const visible = reports.filter(r => (filter === "All" || r.status === filter) &&
    `${r.businessName} ${r.businessLocation} ${r.lgaCode} ${r.saleReportId}`.toLowerCase().includes(query.toLowerCase()));
  const open = reports.filter(r => r.status === "Submitted" || r.status === "Reviewing").length;
  const r = detail?.report;
  return <div className="admin-stack sale-report-view">
    <header><h1>{selectedId ? "Report details" : "Customer sale reports"}</h1><p>Purchases reported by customers for investigation and follow-up.</p></header>
    <section className="admin-panel report-source"><strong>Source: customer-submitted reports</strong><p>Submitted from the customer wallet through “Missing or incorrect receipt?”. Business details and amounts are customer statements, not verified sales. Reports do not create receipts, tax liabilities or reward entries.</p></section>
    {selectedId && <Link to="/admin/reports">← Back to reports</Link>}
    {loading && <p role="status">Loading reports…</p>}
    {error && <div role="alert" className="error">{error} <button onClick={() => setRevision(v => v + 1)}>Reload</button></div>}
    {!loading && !selectedId && !error && <section className="admin-panel">
      <div className="section-heading-row"><h2>Report queue</h2><span>{open} open · {reports.length} loaded</span></div>
      <div className="report-filters">
        <label>Search reports<input value={query} onChange={e => setQuery(e.target.value)} placeholder="Business, location or report ID" /></label>
        <label>Status<select value={filter} onChange={e => setFilter(e.target.value)}>{["All", "Submitted", "Reviewing", "Resolved", "Dismissed"].map(s => <option key={s}>{s}</option>)}</select></label>
      </div>
      <div className="report-table-scroll"><table className="report-table"><thead><tr><th>Business / location</th><th>Issue</th><th>Reported amount</th><th>Status</th><th>Submitted</th><th>Details</th></tr></thead><tbody>
        {visible.map(item => <tr key={item.saleReportId}>
          <td><Link to={`?report=${item.saleReportId}`}>{item.businessName}</Link><small>{item.lgaCode} · {item.businessLocation}</small></td>
          <td>{label(item.issue)}</td><td>₦{item.amount.toLocaleString()}</td><td><span className="status-pill tone-amber">{item.status}</span></td>
          <td>{date(item.createdAt)}</td><td><Link aria-label={`View details for ${item.businessName}`} to={`?report=${item.saleReportId}`}>View details →</Link></td>
        </tr>)}
      </tbody></table></div>
      {visible.length === 0 && <p>No reports match this view.</p>}
      <p className="admin-muted-text">Up to 200 reports, with open reports first. Search and status filters apply to this queue.</p>
    </section>}
    {!loading && r && <>
      <section className="admin-panel">
        <div className="section-heading-row"><h2>{r.businessName}</h2><span className="status-pill tone-amber">{r.status}</span></div>
        <dl className="report-detail-grid">
          <div><dt>Report ID</dt><dd>{r.saleReportId}</dd></div><div><dt>Reported amount</dt><dd>₦{r.amount.toLocaleString()}</dd></div>
          <div><dt>Business location</dt><dd>{r.businessLocation}</dd></div><div><dt>LGA</dt><dd>{r.lgaCode}</dd></div>
          <div><dt>Purchase date</dt><dd>{date(r.purchaseDate)}</dd></div><div><dt>Submitted</dt><dd>{date(r.createdAt)}</dd></div>
          <div><dt>Issue</dt><dd>{label(r.issue)}</dd></div><div><dt>Last reviewed</dt><dd>{r.reviewedAt ? date(r.reviewedAt) : "Not reviewed"}</dd></div>
        </dl>
        <h3>Customer statement</h3><p className="report-statement">{r.details}</p>
        <p className="admin-muted-text">Submitted through a phone-verified customer account. This report has no linked merchant profile or receipt. A customer name and contact number are not stored with the report.</p>
      </section>
      <section className="admin-panel"><h2>Review</h2><p className="report-statement">{r.reviewNote || "No review note yet."}</p>
        {(r.status === "Submitted" || r.status === "Reviewing") ? <form className="report-review-form" onSubmit={e => void review(e, r)}>
          <label>Review outcome<select name="status" key={r.status}>{r.status === "Submitted" ? <option value="Reviewing">Start review</option> : <><option value="Resolved">Resolve after follow-up</option><option value="Dismissed">Dismiss with reason</option></>}</select></label>
          <label>Review note<textarea name="note" required maxLength={1000} rows={4} placeholder="Record the checks performed and the reason for this outcome." /></label>
          <button className="primary" disabled={busy}>{busy ? "Saving…" : "Save review"}</button>
        </form> : <p className="admin-muted-text">This report is closed. Its review history is retained below.</p>}
      </section>
      <section className="admin-panel"><h2>Activity history</h2><ol className="report-timeline">{detail!.history.map(a => <li key={a.eventId}><strong>{label(a.eventType)}</strong><p>By {a.actorName ?? "Not recorded"} · {a.actorRole ?? "Unknown"}</p><time dateTime={a.timestamp}>{date(a.timestamp)}</time><p>{note(a.metadata)}</p></li>)}</ol>{!detail!.history.length && <p>No activity recorded.</p>}</section>
    </>}
  </div>;
}
