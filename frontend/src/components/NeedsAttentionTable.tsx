import StatusPill from "./StatusPill";
import { computeDuration, formatTimestamp } from "../utils/overview";

export type AttentionItem = {
  objectId: string;
  name: string;
  type: "Controller" | "AccessPoint" | "Unknown";
  path: string;
  derivedState: "Offline" | "Unstable" | "Ok" | "Unknown";
  offlineSince?: string;
  lastSeenAt?: string;
  flapCount24h: number;
};

export type AttentionFilters = {
  type: "all" | "controller" | "accessPoint";
  state: "all" | "offline" | "unstable";
  search: string;
};

type Props = {
  items: AttentionItem[];
  filters: AttentionFilters;
  onFiltersChange: (filters: AttentionFilters) => void;
  now: Date;
  maxRows: number;
  dense?: boolean;
};

export default function NeedsAttentionTable({
  items,
  filters,
  onFiltersChange,
  now,
  maxRows,
  dense
}: Props) {
  const filtered = items.filter((item) => {
    if (filters.type === "controller" && item.type !== "Controller") return false;
    if (filters.type === "accessPoint" && item.type !== "AccessPoint") return false;
    if (filters.state === "offline" && item.derivedState !== "Offline") return false;
    if (filters.state === "unstable" && item.derivedState !== "Unstable") return false;

    if (filters.search.trim()) {
      const needle = filters.search.trim().toLowerCase();
      const haystack = `${item.name} ${item.objectId} ${item.path}`.toLowerCase();
      if (!haystack.includes(needle)) return false;
    }

    return true;
  });

  const displayItems = filtered.slice(0, maxRows);

  return (
    <section className="panel">
      <div className="section-header">
        <div>
          <h2>Needs attention</h2>
          <p className="subtle">Showing {displayItems.length} of {filtered.length} items</p>
        </div>
        <div className="filters">
          <div className="toggle-group" role="group" aria-label="Filter by type">
            {[
              { key: "all", label: "All" },
              { key: "controller", label: "Controllers" },
              { key: "accessPoint", label: "Access points" }
            ].map((option) => (
              <button
                key={option.key}
                className={filters.type === option.key ? "toggle active" : "toggle"}
                type="button"
                onClick={() => onFiltersChange({ ...filters, type: option.key as AttentionFilters["type"] })}
                aria-pressed={filters.type === option.key}
              >
                {option.label}
              </button>
            ))}
          </div>
          <div className="toggle-group" role="group" aria-label="Filter by state">
            {[
              { key: "all", label: "All" },
              { key: "offline", label: "Offline" },
              { key: "unstable", label: "Unstable" }
            ].map((option) => (
              <button
                key={option.key}
                className={filters.state === option.key ? "toggle active" : "toggle"}
                type="button"
                onClick={() => onFiltersChange({ ...filters, state: option.key as AttentionFilters["state"] })}
                aria-pressed={filters.state === option.key}
              >
                {option.label}
              </button>
            ))}
          </div>
          <input
            className="search-input"
            type="search"
            placeholder="Search name, id, path"
            value={filters.search}
            onChange={(event) => onFiltersChange({ ...filters, search: event.target.value })}
            aria-label="Search needs attention"
          />
        </div>
      </div>
      {displayItems.length === 0 ? (
        <p className="subtle">No items match the current filters.</p>
      ) : (
        <table className={dense ? "table table-dense" : "table"}>
          <thead>
            <tr>
              <th>State</th>
              <th>Name</th>
              <th>Type</th>
              <th>Offline since</th>
              <th>Duration</th>
              <th>Last seen</th>
              <th>Flaps 24h</th>
              <th>Path</th>
            </tr>
          </thead>
          <tbody>
            {displayItems.map((item) => {
              const duration = computeDuration(item.offlineSince, now);
              const linkTarget = item.type === "Controller" ? `#controller/${item.objectId}` : "#controllers";
              return (
                <tr key={item.objectId}>
                  <td>
                    <StatusPill state={item.derivedState} />
                  </td>
                  <td>
                    <a href={linkTarget} className="link-strong" title={item.objectId}>
                      {item.name}
                    </a>
                  </td>
                  <td>{item.type === "AccessPoint" ? "Access point" : item.type}</td>
                  <td>{formatTimestamp(item.offlineSince)}</td>
                  <td>{duration.label}</td>
                  <td>{formatTimestamp(item.lastSeenAt)}</td>
                  <td>{item.flapCount24h}</td>
                  <td>{item.path || "-"}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </section>
  );
}
