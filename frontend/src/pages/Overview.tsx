import { useEffect, useMemo, useRef, useState } from "react";
import { getEvents, getOverview } from "../api/client";
import type { EventLog, ObjectEntity, StatusSnapshot } from "../api/types";
import ConnectionStrip from "../components/ConnectionStrip";
import KpiCards from "../components/KpiCards";
import NeedsAttentionTable, { type AttentionFilters, type AttentionItem } from "../components/NeedsAttentionTable";
import useSse from "../hooks/useSse";
import {
  computeLongestOffline,
  computeMostFlaps,
  formatDurationShort,
  normalizeDerivedState,
  normalizeObjectType
} from "../utils/overview";

type Props = {
  controlRoomMode: boolean;
};

type OverviewItem = AttentionItem & {
  totalOfflineSeconds24h: number;
};

type ImportantEvent = {
  id: string;
  timestamp: string;
  severity: string;
  message: string;
  source?: string;
};

const emptyFilters: AttentionFilters = { type: "all", state: "all", search: "" };

export default function Overview({ controlRoomMode }: Props) {
  const [dataStale, setDataStale] = useState(false);
  const [lastSync, setLastSync] = useState<string | undefined>();
  const [disconnectedSince, setDisconnectedSince] = useState<string | undefined>();
  const [objects, setObjects] = useState<ObjectEntity[]>([]);
  const [items, setItems] = useState<OverviewItem[]>([]);
  const [filters, setFilters] = useState<AttentionFilters>(emptyFilters);
  const [events, setEvents] = useState<ImportantEvent[]>([]);
  const [now, setNow] = useState(() => new Date());
  const { lastEvent, status: sseStatus } = useSse();

  const objectMapRef = useRef<Map<string, ObjectEntity>>(new Map());
  const lastSortAtRef = useRef<number>(0);
  const sortTimeoutRef = useRef<number | null>(null);

  useEffect(() => {
    const timer = window.setInterval(() => setNow(new Date()), 1000);
    return () => window.clearInterval(timer);
  }, []);

  useEffect(() => {
    let active = true;

    getOverview()
      .then((response) => {
        if (!active) return;
        setDataStale(response.meta.dataStale);
        setLastSync(response.meta.sipassLastSuccessAt);
        setDisconnectedSince(response.meta.disconnectedSince);

        const loadedObjects = response.data.objects ?? [];
        setObjects(loadedObjects);
        objectMapRef.current = new Map(loadedObjects.map((obj) => [obj.objectId, obj]));

        const snapshots = response.data.snapshots ?? [];
        const merged = mergeSnapshots(snapshots, objectMapRef.current);
        setItems(sortByPriority(merged));
        lastSortAtRef.current = Date.now();
      })
      .catch(() => {
        if (!active) return;
        setDataStale(true);
      });

    getEvents()
      .then((response) => {
        if (!active) return;
        const important = response.data
          .filter((entry) => isImportantSeverity(entry.severity))
          .slice(0, 50)
          .map((entry) => toEvent(entry));
        setEvents(important);
      })
      .catch(() => {
        if (!active) return;
        setEvents([]);
      });

    return () => {
      active = false;
    };
  }, []);

  useEffect(() => {
    if (!lastEvent) return;

    if (lastEvent.event === "sipassConnectionChanged") {
      const payload = lastEvent.data as { connected?: boolean } | null;
      if (payload?.connected === true) {
        setDataStale(false);
      } else {
        setDataStale(true);
      }
    }

    if (lastEvent.event === "statusChanged") {
      const payload = lastEvent.data as StatusSnapshot & { type?: string | number };
      const updated = mergeSnapshot(payload, objectMapRef.current);
      setItems((prev) => updateItem(prev, updated));
      scheduleSort();
      addTransitionEvent(updated);
    }
  }, [lastEvent]);

  const counts = useMemo(() => {
    const totals = { Offline: 0, Unstable: 0, Ok: 0 };
    for (const item of items) {
      if (item.derivedState === "Offline") totals.Offline += 1;
      if (item.derivedState === "Unstable") totals.Unstable += 1;
      if (item.derivedState === "Ok") totals.Ok += 1;
    }
    return totals;
  }, [items]);

  const { controllersCount, accessPointsCount } = useMemo(() => {
    if (objects.length === 0) {
      return { controllersCount: 0, accessPointsCount: items.length };
    }
    let controllers = 0;
    let accessPoints = 0;
    for (const obj of objects) {
      const normalized = normalizeObjectType(obj.type);
      if (normalized === "Controller") controllers += 1;
      if (normalized === "AccessPoint") accessPoints += 1;
    }
    return { controllersCount: controllers, accessPointsCount: accessPoints };
  }, [items.length, objects]);

  const offlineItems = useMemo(
    () => items.filter((item) => item.derivedState === "Offline"),
    [items]
  );
  const unstableItems = useMemo(
    () => items.filter((item) => item.derivedState === "Unstable"),
    [items]
  );

  const mostUnstable = useMemo(() => {
    return [...unstableItems].sort((a, b) => b.flapCount24h - a.flapCount24h).slice(0, 10);
  }, [unstableItems]);

  const topOfflineToday = useMemo(() => {
    return [...offlineItems].sort((a, b) => b.totalOfflineSeconds24h - a.totalOfflineSeconds24h).slice(0, 10);
  }, [offlineItems]);

  const hasOfflineTotals = topOfflineToday.some((item) => item.totalOfflineSeconds24h > 0);

  const attentionItems = useMemo(
    () => items.filter((item) => item.derivedState === "Offline" || item.derivedState === "Unstable"),
    [items]
  );

  const longestOffline = computeLongestOffline(offlineItems, now);
  const mostFlaps = computeMostFlaps(unstableItems);

  const maxRows = controlRoomMode ? 12 : 20;

  const handleResetFilters = () => setFilters(emptyFilters);

  return (
    <div className="grid">
      <ConnectionStrip
        dataStale={dataStale}
        lastSuccessAt={lastSync}
        disconnectedSince={disconnectedSince}
        sseStatus={sseStatus}
        now={now}
      />

      <KpiCards
        offlineCount={counts.Offline}
        unstableCount={counts.Unstable}
        okCount={counts.Ok}
        controllersCount={controllersCount}
        accessPointsCount={accessPointsCount}
        longestOfflineLabel={longestOffline}
        mostFlaps={mostFlaps}
        onSelectState={(state) => setFilters((prev) => ({ ...prev, state }))}
        onResetFilters={handleResetFilters}
      />

      <NeedsAttentionTable
        items={attentionItems}
        filters={filters}
        onFiltersChange={setFilters}
        now={now}
        maxRows={maxRows}
        dense={controlRoomMode}
      />

      {!controlRoomMode && (
        <section className="panel grid grid-2">
          <div>
            <h2>Stability & Risk</h2>
            <div className="panel-subgrid">
              <div className="panel panel-inset">
                <h3>Most unstable (24h)</h3>
                {mostUnstable.length === 0 ? (
                  <p className="subtle">No instability detected in the last 24 hours.</p>
                ) : (
                  <ul className="list">
                    {mostUnstable.map((item) => (
                      <li key={item.objectId}>
                        <span>{item.name}</span>
                        <span className="list-meta">{item.flapCount24h} flaps</span>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
              <div className="panel panel-inset">
                <h3>Top offline today</h3>
                {!hasOfflineTotals ? (
                  <p className="subtle">TODO: offline duration samples pending.</p>
                ) : (
                  <ul className="list">
                    {topOfflineToday.map((item) => (
                      <li key={item.objectId}>
                        <span>{item.name}</span>
                        <span className="list-meta">
                          {formatDurationShort(item.totalOfflineSeconds24h)} offline
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </div>
          </div>
          <div>
            <h2>Recent important events</h2>
            {events.length === 0 ? (
              <p className="subtle">No recent warnings or critical events.</p>
            ) : (
              <ul className="list">
                {events.slice(0, 50).map((entry) => (
                  <li key={entry.id}>
                    <span className="event-severity">{entry.severity}</span>
                    <span>{entry.message}</span>
                    <span className="list-meta">{new Date(entry.timestamp).toLocaleString()}</span>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </section>
      )}
    </div>
  );

  function mergeSnapshots(snapshots: StatusSnapshot[], objectMap: Map<string, ObjectEntity>) {
    return snapshots.map((snapshot) => mergeSnapshot(snapshot, objectMap));
  }

  function mergeSnapshot(snapshot: StatusSnapshot, objectMap: Map<string, ObjectEntity>): OverviewItem {
    const object = objectMap.get(snapshot.objectId);
    const name = object?.name ?? snapshot.objectId;
    const path = object?.path ?? "-";
    const snapshotType = (snapshot as { type?: string | number }).type;
    const normalizedType = normalizeObjectType(object?.type ?? snapshotType);
    const derivedState = normalizeDerivedState(snapshot.derivedState);

    return {
      objectId: snapshot.objectId,
      name,
      type: normalizedType,
      path,
      derivedState,
      offlineSince: snapshot.offlineSince,
      lastSeenAt: snapshot.lastSeenAt,
      flapCount24h: snapshot.flapCount24h ?? 0,
      totalOfflineSeconds24h: snapshot.totalOfflineSeconds24h ?? 0
    };
  }

  function updateItem(list: OverviewItem[], updated: OverviewItem) {
    const next = [...list];
    const index = next.findIndex((item) => item.objectId === updated.objectId);
    if (index >= 0) {
      next[index] = { ...next[index], ...updated };
      return next;
    }
    next.push(updated);
    return next;
  }

  function sortByPriority(list: OverviewItem[]) {
    return [...list].sort((a, b) => comparePriority(a, b));
  }

  function comparePriority(a: OverviewItem, b: OverviewItem) {
    const stateRank = (state: OverviewItem["derivedState"]) => {
      if (state === "Offline") return 0;
      if (state === "Unstable") return 1;
      if (state === "Ok") return 2;
      return 3;
    };
    const typeRank = (type: OverviewItem["type"]) => {
      if (type === "Controller") return 0;
      if (type === "AccessPoint") return 1;
      return 2;
    };

    const stateDelta = stateRank(a.derivedState) - stateRank(b.derivedState);
    if (stateDelta !== 0) return stateDelta;

    const typeDelta = typeRank(a.type) - typeRank(b.type);
    if (typeDelta !== 0) return typeDelta;

    if (a.derivedState === "Offline") {
      const aDuration = a.offlineSince ? Date.parse(a.offlineSince) : Number.MAX_SAFE_INTEGER;
      const bDuration = b.offlineSince ? Date.parse(b.offlineSince) : Number.MAX_SAFE_INTEGER;
      return aDuration - bDuration;
    }

    if (a.derivedState === "Unstable") {
      return b.flapCount24h - a.flapCount24h;
    }

    return 0;
  }

  function scheduleSort() {
    const nowMs = Date.now();
    const elapsed = nowMs - lastSortAtRef.current;
    if (elapsed >= 10000) {
      lastSortAtRef.current = nowMs;
      setItems((prev) => sortByPriority(prev));
      return;
    }

    if (sortTimeoutRef.current !== null) return;
    sortTimeoutRef.current = window.setTimeout(() => {
      sortTimeoutRef.current = null;
      lastSortAtRef.current = Date.now();
      setItems((prev) => sortByPriority(prev));
    }, 10000 - elapsed);
  }

  function isImportantSeverity(severity: string) {
    const normalized = severity.toLowerCase();
    return normalized.includes("warn") || normalized.includes("crit");
  }

  function toEvent(entry: EventLog): ImportantEvent {
    return {
      id: `event-${entry.id}`,
      timestamp: entry.timestamp,
      severity: entry.severity.toUpperCase(),
      message: entry.message,
      source: entry.source
    };
  }

  function addTransitionEvent(item: OverviewItem) {
    const message = `${item.name} transitioned to ${item.derivedState}`;
    const entry: ImportantEvent = {
      id: `transition-${item.objectId}-${Date.now()}`,
      timestamp: new Date().toISOString(),
      severity: "STATE",
      message
    };
    setEvents((prev) => [entry, ...prev].slice(0, 50));
  }
}
