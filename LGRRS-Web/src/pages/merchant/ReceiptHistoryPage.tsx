import { useEffect, useState } from "react";
import { useSession } from "../../state/session";
import { api } from "../../api/client";
import type { ReceiptHistoryItem } from "../../api/types";
import ReceiptsTable from "../../components/ReceiptsTable";

export default function ReceiptHistoryPage() {
  const { session } = useSession();
  const [history, setHistory] = useState<ReceiptHistoryItem[]>([]);

  useEffect(() => {
    if (!session) return;
    api.get<ReceiptHistoryItem[]>("/api/merchant/receipts", session.accessToken).then(setHistory).catch(() => {});
  }, [session]);

  if (!session) return null;

  return (
    <section className="step-card">
      <div className="section-heading-row">
        <h2>Receipt History</h2>
      </div>
      <ReceiptsTable receipts={history} />
    </section>
  );
}
