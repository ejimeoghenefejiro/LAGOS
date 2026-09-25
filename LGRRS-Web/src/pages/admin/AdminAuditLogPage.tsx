import { useEffect, useState } from "react";
import { api } from "../../api/client";
import type { AuditEventItem } from "../../api/types";
import { useSession } from "../../state/session";

export default function AdminAuditLogPage() {
  const { session } = useSession();
  const [events, setEvents] = useState<AuditEventItem[]>([]);

  useEffect(() => {
    if (!session) return;
    api.get<AuditEventItem[]>("/api/admin/audit-events?take=50", session.accessToken).then(setEvents).catch(() => {});
  }, [session]);

  return (
    <div className="admin-panel">
      <div className="admin-panel-header">
        <h2>Audit Log</h2>
      </div>
      <table className="admin-table">
        <thead>
          <tr>
            <th>Event</th>
            <th>Performed by</th>
            <th>Role</th>
            <th>Status</th>
            <th>Timestamp</th>
          </tr>
        </thead>
        <tbody>
          {events.map((e) => (
            <tr key={e.eventId}>
              <td>{e.summary}</td>
              <td>{e.actorName ?? "Not recorded"}</td>
              <td>{e.actorRole ?? "Unknown"}</td>
              <td><span className={`status-pill tone-${e.statusTone}`}>{e.statusLabel}</span></td>
              <td>{new Date(e.timestamp).toLocaleString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
