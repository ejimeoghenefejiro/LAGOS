const TONE_BY_STATUS: Record<string, string> = {
  READY_TO_SEND: "green",
  ReadyToSend: "green",
  SENT: "blue",
  Sent: "blue",
  DELIVERED: "green",
  Delivered: "green",
  FAILED: "red",
  Failed: "red",
  Verified: "green",
  Pending: "amber",
  UnderReview: "amber",
  Suspended: "red"
};

export function labelFor(status: string): string {
  return status
    .replace(/_/g, " ")
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/\b\w/g, (c) => c.toUpperCase());
}

export default function StatusPill({ status }: { status: string }) {
  const tone = TONE_BY_STATUS[status] ?? "gray";
  return <span className={`status-pill tone-${tone}`}>{labelFor(status)}</span>;
}
