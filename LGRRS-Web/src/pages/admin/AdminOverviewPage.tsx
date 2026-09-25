import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api } from "../../api/client";
import type { AdminOverview, AuditEventItem, CurrentDraw } from "../../api/types";
import { useSession } from "../../state/session";
import LineChart from "../../components/LineChart";
import DonutChart from "../../components/DonutChart";
import {
  IconClipboardList,
  IconCoin,
  IconGift,
  IconHistory,
  IconShieldCheck,
  IconStore,
  IconTicket,
  IconTrophy,
  IconUsers
} from "../../components/icons";

interface KpiCardDef {
  key: keyof AdminOverview;
  label: string;
  icon: typeof IconTicket;
  tone: string;
  isCurrency?: boolean;
  suffix?: string;
}

const KPI_ROW_1: KpiCardDef[] = [
  { key: "verifiedReceipts", label: "Recorded Valid Receipts", icon: IconTicket, tone: "green" },
  { key: "participatingMerchants", label: "Participating Merchants", icon: IconStore, tone: "blue" },
  { key: "uniqueResidents", label: "Unique Residents", icon: IconUsers, tone: "purple" },
  { key: "totalTransactionValue", label: "Recorded Sales Value", icon: IconCoin, tone: "green", isCurrency: true }
];

const KPI_ROW_2: KpiCardDef[] = [
  { key: "rewardEntriesThisWeek", label: "Reward Entries (This Week)", icon: IconGift, tone: "amber" },
  { key: "fraudAttemptsBlocked", label: "Fraud Attempts Blocked", icon: IconShieldCheck, tone: "red" },
  { key: "prizesWonThisWeek", label: "Prizes Won (This Week)", icon: IconTrophy, tone: "purple" }
];

function formatCurrency(n: number): string {
  return `₦${n.toLocaleString()}`;
}

function auditIcon(eventType: string) {
  if (eventType === "FraudFlagRaised") return IconShieldCheck;
  if (eventType === "RewardEntryCreated") return IconGift;
  if (eventType === "ClaimSubmitted") return IconTrophy;
  return IconClipboardList;
}

export default function AdminOverviewPage() {
  const { session } = useSession();
  const navigate = useNavigate();
  const [overview, setOverview] = useState<AdminOverview | null>(null);
  const [events, setEvents] = useState<AuditEventItem[]>([]);
  const [draw, setDraw] = useState<CurrentDraw | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!session) return;
    api.get<AdminOverview>("/api/admin/overview", session.accessToken).then(setOverview).catch(() => setError("Could not load dashboard."));
    api.get<AuditEventItem[]>("/api/admin/audit-events?take=4", session.accessToken).then(setEvents).catch(() => {});
    api.get<CurrentDraw>("/api/admin/draws/current", session.accessToken).then(setDraw).catch(() => {});
  }, [session]);

  if (!session) return null;

  const renderCard = (def: KpiCardDef) => {
    const metric = overview?.[def.key] as { value: number; weekOverWeekChangePercent?: number | null } | undefined;
    const value = metric?.value ?? 0;
    const delta = metric?.weekOverWeekChangePercent;
    return (
      <div key={def.label} className="admin-kpi-card">
        <div className={`admin-kpi-icon tone-${def.tone}`}>
          <def.icon width={20} height={20} />
        </div>
        <div className="admin-kpi-body">
          <span className="admin-kpi-label">{def.label}</span>
          <span className="admin-kpi-value">{def.isCurrency ? formatCurrency(value) : Math.round(value).toLocaleString()}</span>
          {delta !== null && delta !== undefined && (
            <span className={`admin-kpi-delta ${delta >= 0 ? "up" : "down"}`}>
              {delta >= 0 ? "↑" : "↓"} {Math.abs(delta)}% vs last week
            </span>
          )}
        </div>
      </div>
    );
  };

  return (
    <div className="admin-stack">
      {error && <p className="error">{error}</p>}

      <div className="admin-kpi-grid">
        {KPI_ROW_1.map(renderCard)}
        {KPI_ROW_2.map(renderCard)}
        <div className="admin-kpi-card">
          <div className="admin-kpi-icon tone-amber">
            <IconCoin width={20} height={20} />
          </div>
          <div className="admin-kpi-body">
            <span className="admin-kpi-label">Prize Pool (Current Draw)</span>
            <span className="admin-kpi-value">{overview ? formatCurrency(overview.currentPrizePool) : "-"}</span>
            {overview?.drawClosesInDays != null && (
              <span className="admin-kpi-delta neutral">Draw closes in {overview.drawClosesInDays} days</span>
            )}
          </div>
        </div>
      </div>

      <div className="admin-grid-3">
        <div className="admin-panel span-2">
          <div className="admin-panel-header">
            <h2>Receipt Trend (Last 6 Weeks)</h2>
          </div>
          {overview && <LineChart points={overview.receiptTrend} />}
        </div>

        <div className="admin-panel">
          <div className="admin-panel-header">
            <h2>Top Merchants (by Receipts)</h2>
          </div>
          <ol className="admin-top-merchants">
            {overview?.topMerchants.map((m, i) => (
              <li key={m.merchantId} className="admin-clickable-row" onClick={() => navigate(`/admin/merchants/${m.merchantId}`)}>
                <span className="admin-rank">{i + 1}</span>
                <span className="admin-merchant-info">
                  <strong>{m.businessName}</strong>
                  <span className="admin-muted-text">{m.lgaCode} GRA</span>
                </span>
                <span className="admin-merchant-count">{m.receiptsIssued.toLocaleString()}</span>
              </li>
            ))}
          </ol>
          <Link to="/admin/merchants" className="admin-link">View all merchants</Link>
        </div>
      </div>

      <div className="admin-grid-3">
        <div className="admin-panel span-2">
          <div className="admin-panel-header">
            <h2><IconHistory width={16} height={16} /> Recent Activities</h2>
            <Link to="/admin/audit-log" className="admin-link">View all</Link>
          </div>
          <ul className="admin-activity-list">
            {events.map((e) => {
              const Icon = auditIcon(e.eventType);
              return (
                <li key={e.eventId}>
                  <span className={`admin-activity-icon tone-${e.statusTone}`}><Icon width={14} height={14} /></span>
                  <span className="admin-activity-summary">{e.summary}</span>
                  <span className="admin-muted-text">{new Date(e.timestamp).toLocaleString(undefined, { day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit" })}</span>
                  <span className={`status-pill tone-${e.statusTone === "red" ? "red" : e.statusTone === "green" ? "green" : e.statusTone === "purple" ? "purple" : "blue"}`}>{e.statusLabel}</span>
                </li>
              );
            })}
            {events.length === 0 && <p className="admin-muted-text">No activity recorded yet.</p>}
          </ul>
        </div>

        <div className="admin-panel">
          <div className="admin-panel-header">
            <h2>Fraud &amp; Risk Summary</h2>
          </div>
          {overview && <DonutChart summary={overview.fraudRisk} />}
          <Link to="/admin/fraud" className="admin-link-button">View fraud &amp; risk details</Link>
        </div>
      </div>

      {draw && (
        <div className="admin-panel admin-draw-panel">
          <div className="admin-panel-header">
            <h2>Current Draw ({draw.type})</h2>
            <span className="status-pill tone-green">{draw.status}</span>
          </div>
          <dl className="admin-draw-details">
            <dt>Draw Period</dt>
            <dd>{new Date(draw.startDate).toLocaleDateString()} &ndash; {new Date(draw.endDate).toLocaleDateString()}</dd>
            <dt>Eligible Entries</dt>
            <dd>{draw.eligibleEntries.toLocaleString()}</dd>
            <dt>Prize Pool</dt>
            <dd>{formatCurrency(draw.prizeBudget)}</dd>
            <dt>Draw Date</dt>
            <dd>{draw.drawDate ? new Date(draw.drawDate).toLocaleString() : new Date(draw.endDate).toLocaleDateString()}</dd>
          </dl>
          <button className="admin-manage-draw-button" onClick={() => navigate("/admin/draws")}>Manage Draw</button>
        </div>
      )}
    </div>
  );
}
