import { useEffect, useState } from "react";
import { api } from "../../api/client";
import type { AdminReceiptItem } from "../../api/types";
import { useSession } from "../../state/session";

const STATUS_OPTIONS = ["All", "Valid", "Voided", "Refunded", "UnderReview"];

export default function AdminReceiptsPage() {
  const { session } = useSession();
  const [receipts, setReceipts] = useState<AdminReceiptItem[]>([]);
  const [status, setStatus] = useState("All");
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!session) return;
    setLoading(true);
    const params = new URLSearchParams();
    if (status !== "All") params.set("status", status);
    if (search.trim()) params.set("search", search.trim());
    api
      .get<AdminReceiptItem[]>(`/api/admin/receipts?${params.toString()}`, session.accessToken)
      .then(setReceipts)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [session, status, search]);

  return (
    <div className="admin-panel">
      <div className="admin-panel-header">
        <h2>Receipts</h2>
      </div>

      <div className="admin-filter-row">
        <input
          className="admin-search-input"
          placeholder="Search by merchant or item/service..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <select className="admin-search-input" value={status} onChange={(e) => setStatus(e.target.value)}>
          {STATUS_OPTIONS.map((s) => <option key={s} value={s}>{s}</option>)}
        </select>
      </div>

      <table className="admin-table">
        <thead>
          <tr>
            <th>Receipt Ref</th>
            <th>Merchant</th>
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
              <td>{r.merchantName} <span className="admin-muted-text">{r.lgaCode}</span></td>
              <td>{r.customerName ?? "-"} <span className="admin-muted-text">{r.customerPhoneMasked}</span></td>
              <td>{r.itemService}</td>
              <td>₦{r.amount.toLocaleString()}</td>
              <td><span className={`status-pill tone-${r.status === "Valid" ? "green" : r.status === "UnderReview" ? "amber" : "red"}`}>{r.status}</span></td>
              <td>{new Date(r.transactionDate).toLocaleString()}</td>
            </tr>
          ))}
          {!loading && receipts.length === 0 && (
            <tr><td colSpan={7} className="admin-muted-text">No receipts match this filter.</td></tr>
          )}
        </tbody>
      </table>
    </div>
  );
}
