import { useEffect, useState } from "react";
import { useSession } from "../../state/session";
import { api } from "../../api/client";
import type { ReceiptHistoryItem } from "../../api/types";
import ReceiptsTable from "../../components/ReceiptsTable";

export default function MerchantDashboardPage() {
  const { session } = useSession();
  const [history, setHistory] = useState<ReceiptHistoryItem[]>([]);

  useEffect(() => {
    if (!session) return;
    api.get<ReceiptHistoryItem[]>("/api/merchant/receipts", session.accessToken).then(setHistory).catch(() => {});
  }, [session]);

  if (!session) return null;

  const totalSales = history.reduce((sum, r) => sum + r.amount, 0);

  return (
    <div className="stack-lg">
      <section className="step-card">
        <h2>Welcome back, {session.displayName}</h2>
        <div className="kpi-grid">
          <div className="kpi-card">
            <span className="kpi-label">Receipts Issued</span>
            <span className="kpi-value">{history.length}</span>
          </div>
          <div className="kpi-card">
            <span className="kpi-label">Total Sales</span>
            <span className="kpi-value">₦{totalSales.toLocaleString()}</span>
          </div>
        </div>
      </section>

      <section className="step-card">
        <div className="section-heading-row">
          <h2>Recent Receipts</h2>
        </div>
        <ReceiptsTable receipts={history.slice(0, 5)} />
      </section>
    </div>
  );
}
