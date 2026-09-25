import { useEffect, useState } from "react";
import { Link, Navigate } from "react-router-dom";
import { api } from "../../api/client";
import { useSession } from "../../state/session";
import { Roles } from "../../state/roles";

interface WalletReceipt {
  receiptId: string; merchantName: string; itemService: string;
  amount: number; transactionDate: string; status: string; entryStatus: string;
}
export default function ReceiptWalletPage() {
  const { session } = useSession();
  const [receipts, setReceipts] = useState<WalletReceipt[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    if (session?.role !== Roles.Consumer) return;
    api.get<WalletReceipt[]>("/api/consumer/receipts", session.accessToken)
      .then(setReceipts).catch(() => setError("Could not load receipts. Please reload to try again."))
      .finally(() => setLoading(false));
  }, [session]);
  if (session?.role !== Roles.Consumer) return <Navigate to="/check/login" replace />;
  return <div className="consumer-page-stack">
    <div><h1>My receipts</h1><p>Ask for a receipt with every purchase. Receipts issued to your verified phone appear here.</p></div>
    <div className="consumer-info-box"><p>Each recorded purchase makes business activity visible to the programme. Eligible receipts enter the draw automatically. These records are not proof of tax payment.</p></div>
    <Link className="primary" to="/check/report">Missing or incorrect receipt?</Link>
    <Link className="primary" to="/check/claim">Claim paper receipt</Link>
    {loading && <p role="status">Loading your receipts…</p>}
    {error && <p className="error" role="alert">{error}</p>}
    {!loading && !error && receipts.length === 0 && <p>No receipts yet. Ask the merchant to use the phone number you signed in with.</p>}
    {receipts.map(r => <article className="consumer-card" key={r.receiptId}>
      <div className="section-heading-row"><strong>{r.merchantName}</strong><strong>₦{r.amount.toLocaleString()}</strong></div>
      <p>{r.itemService}</p><p className="consumer-muted">{new Date(r.transactionDate).toLocaleDateString()} · {r.status}</p>
      <p className="status-pill tone-green">{r.entryStatus}</p>
      <div><Link to={`/check?receipt=${encodeURIComponent(r.receiptId)}`}>Check receipt and prize →</Link></div>
    </article>)}
    <p className="consumer-muted">Showing your latest 100 receipts. Full ticket references stay private to your account.</p>
  </div>;
}
