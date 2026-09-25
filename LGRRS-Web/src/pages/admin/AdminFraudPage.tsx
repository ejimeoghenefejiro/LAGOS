import { useEffect, useState } from "react";
import { api } from "../../api/client";
import type { FraudFlagItem } from "../../api/types";
import { useSession } from "../../state/session";

export default function AdminFraudPage() {
  const { session } = useSession();
  const [flags, setFlags] = useState<FraudFlagItem[]>([]);

  useEffect(() => {
    if (!session) return;
    api.get<FraudFlagItem[]>("/api/admin/fraud-flags", session.accessToken).then(setFlags).catch(() => {});
  }, [session]);

  return (
    <div className="admin-panel">
      <div className="admin-panel-header">
        <h2>Fraud &amp; Risk</h2>
      </div>
      <table className="admin-table">
        <thead>
          <tr>
            <th>Rule</th>
            <th>Entity</th>
            <th>Severity</th>
            <th>Status</th>
            <th>Created</th>
          </tr>
        </thead>
        <tbody>
          {flags.map((f) => (
            <tr key={f.flagId}>
              <td>{f.ruleCode}</td>
              <td>{f.entityType}</td>
              <td><span className={`status-pill tone-${f.severity === "High" ? "red" : f.severity === "Medium" ? "amber" : "green"}`}>{f.severity}</span></td>
              <td>{f.status}</td>
              <td>{new Date(f.createdAt).toLocaleString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
