import type { ReceiptHistoryItem } from "../api/types";
import DeliveryIcons from "./DeliveryIcons";
import StatusPill from "./StatusPill";
import { IconMoreVertical } from "./icons";

export default function ReceiptsTable({ receipts }: { receipts: ReceiptHistoryItem[] }) {
  if (receipts.length === 0) {
    return <p className="muted">No receipts yet. Issue your first receipt above.</p>;
  }

  return (
    <div className="table-scroll">
      <table className="receipts-table">
        <thead>
          <tr>
            <th>Receipt Ref</th>
            <th>Customer</th>
            <th>Service</th>
            <th>Amount</th>
            <th>Delivery</th>
            <th>Status</th>
            <th>Date &amp; Time</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {receipts.map((r) => (
            <tr key={r.receiptRefMasked + r.transactionDate}>
              <td className="mono">{r.receiptRefMasked}</td>
              <td>
                <div className="customer-cell">
                  <span>{r.customerName ?? "—"}</span>
                  <span className="muted small">{r.customerPhoneMasked}</span>
                </div>
              </td>
              <td>{r.itemService}</td>
              <td>₦{r.amount.toLocaleString()}</td>
              <td><DeliveryIcons channels={r.channels} /></td>
              <td><StatusPill status={r.deliveryStatus} /></td>
              <td className="muted small">
                {new Date(r.transactionDate).toLocaleString(undefined, {
                  day: "2-digit",
                  month: "short",
                  hour: "2-digit",
                  minute: "2-digit"
                })}
              </td>
              <td>
                <button className="icon-button" aria-label="More options">
                  <IconMoreVertical width={16} height={16} />
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
