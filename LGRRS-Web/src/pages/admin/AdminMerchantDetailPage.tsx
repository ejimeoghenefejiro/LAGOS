import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api } from "../../api/client";
import type { AdminMerchantDetail, AdminMerchantReceiptItem } from "../../api/types";
import { useSession } from "../../state/session";
import { IconChevronDown } from "../../components/icons";

export default function AdminMerchantDetailPage() {
  const { session } = useSession();
  const { merchantId } = useParams();
  const [merchant, setMerchant] = useState<AdminMerchantDetail | null>(null);
  const [receipts, setReceipts] = useState<AdminMerchantReceiptItem[]>([]);
  const [error, setError] = useState<string | null>(null);

  const [action, setAction] = useState("Approve");
  const [reason, setReason] = useState("");
  const [saving, setSaving] = useState(false);
  const [reviewError, setReviewError] = useState("");
  const [history, setHistory] = useState<{ eventId: string; timestamp: string; actorName: string; metadata: string }[]>([]);
  const [revision, setRevision] = useState(0);
  async function review() {
    if (!merchant || !session) return;
    setSaving(true); setReviewError("");
    try { await api.post(`/api/admin/merchants/${merchantId}/review`, { action, reason, expectedStatus: merchant.status }, session.accessToken); setReason(""); setRevision(v => v + 1); }
    catch (e) { setReviewError(e instanceof Error ? e.message : "Could not save review."); }
    finally { setSaving(false); }
  }
  useEffect(() => {
    if (!session || !merchantId) return;
    api.get<AdminMerchantDetail>(`/api/admin/merchants/${merchantId}`, session.accessToken).then(m => { setMerchant(m); setAction(m.status === "Verified" ? "Suspend" : "Approve"); }).catch(() => setError("Could not load merchant."));
    api.get<AdminMerchantReceiptItem[]>(`/api/admin/merchants/${merchantId}/receipts`, session.accessToken).then(setReceipts).catch(() => {});
    api.get<typeof history>(`/api/admin/merchants/${merchantId}/reviews`, session.accessToken).then(setHistory).catch(() => setReviewError("Could not load review history."));
  }, [session, merchantId, revision]);

  if (error) return <p className="error">{error}</p>;
  if (!merchant) return <div className="admin-panel">Loading...</div>;

  return (
    <div className="admin-stack">
      <Link to="/admin/merchants" className="admin-link">
        <IconChevronDown width={14} height={14} style={{ transform: "rotate(90deg)" }} /> Back to Merchants
      </Link>

      <div className="admin-panel">
        <div className="admin-panel-header">
          <h2>{merchant.businessName}</h2>
          <span className={`status-pill tone-${merchant.status === "Verified" ? "green" : merchant.status === "Suspended" ? "red" : "amber"}`}>{merchant.status}</span>
        </div>
        <p className="admin-muted-text">
          {merchant.businessType} &middot; {merchant.lgaCode}
          {merchant.lgrrsSystemId && <> &middot; {merchant.lgrrsSystemId}</>}
        </p>

        <dl className="report-detail-grid"><div><dt>Business address</dt><dd>{merchant.businessAddress || "Not provided — this older record has no saved address"}</dd></div><div><dt>Registered phone</dt><dd>{merchant.phoneNumber}</dd></div></dl>
        <div className="admin-kpi-grid" style={{ marginTop: 16 }}>
          <div className="admin-kpi-card">
            <div className="admin-kpi-body">
              <span className="admin-kpi-label">Receipts Issued</span>
              <span className="admin-kpi-value">{merchant.receiptsIssued.toLocaleString()}</span>
            </div>
          </div>
          <div className="admin-kpi-card">
            <div className="admin-kpi-body">
              <span className="admin-kpi-label">Sales Value</span>
              <span className="admin-kpi-value">₦{merchant.salesValue.toLocaleString()}</span>
            </div>
          </div>
          <div className="admin-kpi-card">
            <div className="admin-kpi-body">
              <span className="admin-kpi-label">Average Sale</span>
              <span className="admin-kpi-value">₦{merchant.averageSale.toLocaleString()}</span>
            </div>
          </div>
          <div className="admin-kpi-card">
            <div className="admin-kpi-body">
              <span className="admin-kpi-label">Reward Entries</span>
              <span className="admin-kpi-value">{merchant.rewardEntries.toLocaleString()}</span>
            </div>
          </div>
        </div>
      </div>

      <section className="admin-panel"><h2>Administrator review</h2>
        <p>Approval enables receipt issuance and POS keys. This is LGRRS programme approval, not LIRS tax verification.</p>
        {merchant.reviewReason && <p>Last decision: {merchant.reviewReason}</p>}
        <form className="report-review-form" onSubmit={e => { e.preventDefault(); void review(); }}>
          <label>Action<select value={action} onChange={e => setAction(e.target.value)}><option value="Approve" disabled={merchant.status === "Verified"}>Approve / reinstate</option><option value="RequestCorrections" disabled={!["Pending", "UnderReview"].includes(merchant.status)}>Request corrections</option><option value="Reject" disabled={!["Pending", "UnderReview"].includes(merchant.status)}>Reject</option><option value="Suspend" disabled={merchant.status !== "Verified"}>Suspend</option></select></label>
          <label>Reason<textarea required maxLength={1000} rows={3} value={reason} onChange={e => setReason(e.target.value)} placeholder="Record the checks and reason for this decision." /></label>
          <button className="primary" disabled={saving || !reason.trim()}>{saving ? "Saving…" : "Save decision"}</button>
        </form>{reviewError && <p role="alert" className="error">{reviewError}</p>}
        <h3>Decision history</h3><ol className="report-timeline">{history.map(h => { let d; try { d = JSON.parse(h.metadata); } catch { d = {}; } return <li key={h.eventId}><strong>{d.PreviousStatus} → {d.NewStatus}</strong><p>{d.Reason}</p><small>{h.actorName} · {new Date(h.timestamp).toLocaleString()}</small></li>; })}</ol>{!history.length && <p>No administrator decisions recorded.</p>}
      </section>
      <div className="admin-panel">
        <div className="admin-panel-header">
          <h2>Receipts</h2>
        </div>
        <table className="admin-table">
          <thead>
            <tr>
              <th>Receipt Ref</th>
              <th>Customer</th>
              <th>Service</th>
              <th>Amount</th>
              <th>Status</th>
              <th>Date &amp; Time</th>
            </tr>
          </thead>
          <tbody>
            {receipts.map((r) => (
              <tr key={r.receiptRefMasked + r.transactionDate}>
                <td>{r.receiptRefMasked}</td>
                <td>{r.customerName ?? "-"} <span className="admin-muted-text">{r.customerPhoneMasked}</span></td>
                <td>{r.itemService}</td>
                <td>₦{r.amount.toLocaleString()}</td>
                <td><span className={`status-pill tone-${r.status === "Valid" ? "green" : r.status === "UnderReview" ? "amber" : "red"}`}>{r.status}</span></td>
                <td>{new Date(r.transactionDate).toLocaleString()}</td>
              </tr>
            ))}
            {receipts.length === 0 && (
              <tr><td colSpan={6} className="admin-muted-text">No receipts yet.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
