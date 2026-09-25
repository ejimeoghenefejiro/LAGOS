import { useEffect, useState } from "react";
import { api, ApiError } from "../../api/client";
import type { CurrentDraw, DrawWinnerItem, RunDrawResponse } from "../../api/types";
import { useSession } from "../../state/session";
import { IconTrophy } from "../../components/icons";

export default function AdminDrawsPage() {
  const { session } = useSession();
  const [draw, setDraw] = useState<CurrentDraw | null>(null);
  const [winners, setWinners] = useState<DrawWinnerItem[]>([]);
  const [running, setRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notFound, setNotFound] = useState(false);
  const [history, setHistory] = useState<CurrentDraw[]>([]);
  const [loading, setLoading] = useState(true);
  const [drawType, setDrawType] = useState("Weekly");
  const [budget, setBudget] = useState("50000");
  const [confirmRun, setConfirmRun] = useState(false);

  const selectDraw = async (selected: CurrentDraw) => {
    if (!session) return;
    setLoading(true); setError(null); setConfirmRun(false); setWinners([]);
    setDraw(selected);
    try {
      setWinners(await api.get<DrawWinnerItem[]>(`/api/admin/draws/${selected.drawPeriodId}/winners`, session.accessToken));
    } catch { setError("Could not load winners. Please refresh."); }
    finally { setLoading(false); }
  };

  const load = async () => {
    if (!session) return;
    try {
      const draws = await api.get<CurrentDraw[]>("/api/admin/draws", session.accessToken);
      setHistory(draws);
      setNotFound(!draws.some(d => d.status === "Open"));
      const selected = draws.find(d => d.status === "Open") ?? draws[0];
      if (selected) await selectDraw(selected);
      else { setDraw(null); setWinners([]); }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not load draws.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [session]);

  const createDraw = async () => {
    if (!session) return;
    setRunning(true); setError(null);
    try {
      const created = await api.post<CurrentDraw>("/api/admin/draws", { type: drawType, prizeBudget: Number(budget) }, session.accessToken);
      setHistory(prev => [created, ...prev]); setDraw(created); setWinners([]);
      setNotFound(false); setConfirmRun(false);
    } catch (err) { setError(err instanceof Error ? err.message : "Could not create draw."); }
    finally { setRunning(false); }
  };

  const runDraw = async () => {
    if (!draw || !session) return;
    setError(null);
    setRunning(true);
    try {
      const result = await api.post<RunDrawResponse>(`/api/admin/draws/${draw.drawPeriodId}/run`, undefined, session.accessToken);
      setWinners(result.winners);
      // Don't refetch "current draw" here — it just left Open status, so that endpoint
      // would 404. Patch the draw we already have in state instead of losing the result.
      setDraw({ ...draw, status: "Published", drawDate: new Date().toISOString() });
      setHistory(prev => prev.map(d => d.drawPeriodId === draw.drawPeriodId ? { ...d, status: "Published", drawDate: new Date().toISOString() } : d));
      setNotFound(true); setConfirmRun(false);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not run the draw.");
    } finally {
      setRunning(false);
    }
  };

  return (
    <div className="admin-stack">
      <div><h1>Draws &amp; Winners</h1><p>Create a prize period, collect eligible receipts, and publish the results.</p></div>
      {error && <p className="error" role="alert">{error}</p>}
      {loading && <p role="status">Loading draws…</p>}
      {!loading && notFound && <section className="admin-panel">
        <h2>Open a new draw</h2>
        <p>No draw is open. Create a period so new qualifying receipts can enter automatically.</p>
        <form className="stack-md" onSubmit={e => { e.preventDefault(); void createDraw(); }}>
          <div className="form-grid">
            <label className="field-label">Draw period<select value={drawType} onChange={e => setDrawType(e.target.value)} disabled={running}>
              <option>Weekly</option><option>Monthly</option>
            </select></label>
            <label className="field-label">Prize budget (₦)<input type="number" min="37500" max="1000000000" step="0.01" required value={budget} onChange={e => setBudget(e.target.value)} disabled={running} /></label>
          </div>
          <p>Starts immediately and accepts receipts for {drawType === "Weekly" ? "7 days" : "one calendar month"}. Existing receipts are not re-entered.</p>
          <p className="admin-muted-text">The current demo awards up to one ₦2,500, ₦10,000 and ₦25,000 prize. Minimum budget: ₦37,500. No money is transferred.</p>
          <button className="primary" disabled={running}>{running ? "Creating…" : "Create draw period"}</button>
        </form>
      </section>}
      {history.length > 0 && <label className="field-label">Draw history
        <select value={draw?.drawPeriodId ?? ""} disabled={loading || running} onChange={e => {
          const selected = history.find(d => d.drawPeriodId === e.target.value);
          if (selected) void selectDraw(selected);
        }}>
          {history.map(d => <option key={d.drawPeriodId} value={d.drawPeriodId}>{d.type} · {new Date(d.startDate).toLocaleDateString()} · {d.status} · {d.drawPeriodId.slice(0, 8)}</option>)}
        </select>
      </label>}
      {draw && <>
      <div className="admin-panel">
        <div className="admin-panel-header">
          <h2>Selected Draw ({draw.type})</h2>
          <span className={`status-pill tone-${draw.status === "Open" ? "amber" : "green"}`}>{draw.status}</span>
        </div>
        <dl className="admin-draw-details">
          <dt>Draw Period</dt>
          <dd>{new Date(draw.startDate).toLocaleDateString()} &ndash; {new Date(draw.endDate).toLocaleDateString()}</dd>
          <dt>Eligible Entries</dt>
          <dd>{draw.eligibleEntries.toLocaleString()}</dd>
          <dt>Prize Pool</dt>
          <dd>₦{draw.prizeBudget.toLocaleString()}</dd>
          <dt>Draw Date</dt>
          <dd>{draw.drawDate ? new Date(draw.drawDate).toLocaleString() : "Not run yet"}</dd>
        </dl>
        {draw.status === "Open" ? (
          <div>
            <p>{draw.eligibleEntries === 0 ? "Waiting for eligible receipts. Issue a new receipt from a verified merchant during this period." : "Ready to select winners from eligible receipts."}</p>
            {confirmRun ? <div className="stack-md">
              <p>Run and publish this demo draw now? This closes entry collection immediately, even if the period has not ended. Published results cannot be rerun.</p>
              <div className="step-actions">
                <button onClick={() => setConfirmRun(false)} disabled={running}>Cancel</button>
                <button className="primary" onClick={runDraw} disabled={running}>{running ? "Publishing…" : "Confirm and publish"}</button>
              </div>
            </div> : <button className="admin-manage-draw-button" onClick={() => setConfirmRun(true)} disabled={running || loading || draw.eligibleEntries === 0}>Run demo draw</button>}
            <button onClick={load} disabled={running || loading}>Refresh entries</button>
          </div>
        ) : (
          <p className="admin-muted-text" style={{ marginTop: 12 }}>This draw has already been run and published.</p>
        )}
      </div>

      <div className="admin-panel">
        <div className="admin-panel-header">
          <h2><IconTrophy width={16} height={16} /> Winners</h2>
        </div>
        {winners.length === 0 ? (
          <p className="admin-muted-text">No winners yet for this draw.</p>
        ) : (
          <div className="table-scroll"><table className="admin-table">
            <thead>
              <tr>
                <th>Prize Tier</th>
                <th>Prize Amount</th>
                <th>Receipt Ref</th>
                <th>Merchant</th>
                <th>Claimed</th>
                <th>Drawn At</th>
              </tr>
            </thead>
            <tbody>
              {winners.map((w) => (
                <tr key={w.drawResultId}>
                  <td>{w.prizeTier}</td>
                  <td>₦{w.prizeAmount.toLocaleString()}</td>
                  <td>{w.receiptRefMasked}</td>
                  <td>{w.merchantName} <span className="admin-muted-text">{w.lgaCode}</span></td>
                  <td><span className={`status-pill tone-${w.hasClaim ? "green" : "amber"}`}>{w.hasClaim ? "Claimed" : "Unclaimed"}</span></td>
                  <td>{new Date(w.drawTimestamp).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
          </table></div>
        )}
      </div>
      </>}

      <div className="admin-panel">
        <div className="admin-panel-header">
          <h2>How Winners Are Selected</h2>
        </div>
        <ul className="admin-algo-notes">
          <li>Receipt amount decides which prize tier an entry can win &mdash; small purchases can only win the small-tier prize, never the jackpot.</li>
          <li>At most one winner per merchant per draw, to spread prizes across the LGA rather than one location.</li>
          <li>Customers who already won this calendar month get 1/20th the selection weight of a fresh customer, so new winners keep surfacing.</li>
          <li>Splitting a purchase into many small receipts to farm small-tier entries gets detected and those entries are excluded.</li>
        </ul>
      </div>
    </div>
  );
}
