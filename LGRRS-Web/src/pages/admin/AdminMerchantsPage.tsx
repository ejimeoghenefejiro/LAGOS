import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api } from "../../api/client";
import type { AdminMerchant } from "../../api/types";
import { useSession } from "../../state/session";

export default function AdminMerchantsPage() {
  const { session } = useSession();
  const navigate = useNavigate();
  const [merchants, setMerchants] = useState<AdminMerchant[]>([]);

  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("All");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const filtered = merchants.filter(m => (status === "All" || m.status === status) && `${m.businessName} ${m.businessType} ${m.lgaCode}`.toLowerCase().includes(query.trim().toLowerCase()));

  useEffect(() => {
    if (!session) return;
    api.get<AdminMerchant[]>("/api/admin/merchants", session.accessToken).then(setMerchants).catch(() => setError("Could not load merchants. Please reload to try again.")).finally(() => setLoading(false));
  }, [session]);

  return (
    <div className="admin-panel">
      <div className="admin-panel-header">
        <h2>Merchants</h2>
      </div>
      <div className="merchant-list-filters">
        <input aria-label="Search merchants" placeholder="Search by business name, type or LGA…" value={query} onChange={e => setQuery(e.target.value)} />
        <select aria-label="Filter merchants by status" value={status} onChange={e => setStatus(e.target.value)}>
          {["All", "Pending", "Verified", "UnderReview", "Rejected", "Suspended"].map(s => <option key={s} value={s}>{s === "UnderReview" ? "Under review" : s}</option>)}
        </select>
      </div>
      {loading && <p role="status">Loading merchants…</p>}
      {error && <p role="alert" className="error">{error}</p>}
      {!loading && !error && <p className="admin-muted-text">{filtered.length} of {merchants.length} merchants</p>}
      <div className="report-table-scroll"><table className="admin-table">
        <thead>
          <tr>
            <th>Business Name</th>
            <th>Type</th>
            <th>LGA</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          {filtered.map((m) => (
            <tr key={m.merchantId} className="admin-table-row-clickable" onClick={() => navigate(`/admin/merchants/${m.merchantId}`)}>
              <td><Link to={`/admin/merchants/${m.merchantId}`}>{m.businessName}</Link></td>
              <td>{m.businessType}</td>
              <td>{m.lgaCode}</td>
              <td><span className={`status-pill tone-${m.status === "Verified" ? "green" : m.status === "Suspended" ? "red" : "amber"}`}>{m.status}</span></td>
            </tr>
          ))}
        </tbody>
      </table></div>
      {!loading && !error && filtered.length === 0 && <p>No merchants match your search.</p>}
    </div>
  );
}
