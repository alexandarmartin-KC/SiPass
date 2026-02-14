type Props = {
  offlineCount: number;
  unstableCount: number;
  okCount: number;
  controllersCount: number;
  accessPointsCount: number;
  longestOfflineLabel: string;
  mostFlaps: number;
  onSelectState: (state: "all" | "offline" | "unstable") => void;
  onResetFilters: () => void;
};

export default function KpiCards({
  offlineCount,
  unstableCount,
  okCount,
  controllersCount,
  accessPointsCount,
  longestOfflineLabel,
  mostFlaps,
  onSelectState,
  onResetFilters
}: Props) {
  return (
    <section className="kpi-grid">
      <button className="kpi-card kpi-card--offline" type="button" onClick={() => onSelectState("offline")}
        aria-label="Filter to offline">
        <div className="kpi-label">OFFLINE</div>
        <div className="kpi-value">{offlineCount}</div>
        <div className="kpi-sub">Longest offline: {longestOfflineLabel}</div>
      </button>
      <button className="kpi-card kpi-card--unstable" type="button" onClick={() => onSelectState("unstable")}
        aria-label="Filter to unstable">
        <div className="kpi-label">UNSTABLE</div>
        <div className="kpi-value">{unstableCount}</div>
        <div className="kpi-sub">Most flaps: {mostFlaps}</div>
      </button>
      <button className="kpi-card kpi-card--ok" type="button" onClick={() => onSelectState("all")}
        aria-label="Show all attention items">
        <div className="kpi-label">OK</div>
        <div className="kpi-value">{okCount}</div>
        <div className="kpi-sub">Stable devices</div>
      </button>
      <button className="kpi-card kpi-card--total" type="button" onClick={onResetFilters}
        aria-label="Reset filters">
        <div className="kpi-label">TOTAL MONITORED</div>
        <div className="kpi-value">{controllersCount + accessPointsCount}</div>
        <div className="kpi-sub">
          Controllers: {controllersCount} • Access points: {accessPointsCount}
        </div>
      </button>
    </section>
  );
}
