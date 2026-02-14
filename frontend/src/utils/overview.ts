import type { DerivedState, ObjectType } from "../api/types";

export type NormalizedObjectType = "Controller" | "AccessPoint" | "Unknown";

const objectTypeMap: Record<number, NormalizedObjectType> = {
  0: "Controller",
  1: "AccessPoint"
};

const derivedStateMap: Record<number, DerivedState> = {
  0: "Ok",
  1: "Unstable",
  2: "Offline",
  3: "Unknown",
  4: "Unknown"
};

export function normalizeObjectType(value: ObjectType | string | number | undefined): NormalizedObjectType {
  if (typeof value === "number") {
    return objectTypeMap[value] ?? "Unknown";
  }
  if (value === "Controller" || value === "AccessPoint") {
    return value;
  }
  return "Unknown";
}

export function normalizeDerivedState(value: DerivedState | string | number | undefined): DerivedState {
  if (typeof value === "number") {
    return derivedStateMap[value] ?? "Unknown";
  }
  if (value === "Ok" || value === "Unstable" || value === "Offline" || value === "Unknown") {
    return value;
  }
  return "Unknown";
}

export function computeDuration(offlineSince: string | undefined, now: Date) {
  if (!offlineSince) {
    return { seconds: 0, label: "-" };
  }
  const sinceMs = Date.parse(offlineSince);
  if (Number.isNaN(sinceMs)) {
    return { seconds: 0, label: "-" };
  }
  const seconds = Math.max(0, Math.floor((now.getTime() - sinceMs) / 1000));
  return { seconds, label: formatDurationShort(seconds) };
}

export function computeLongestOffline(
  items: Array<{ offlineSince?: string }>,
  now: Date
) {
  let maxSeconds = 0;
  for (const item of items) {
    if (!item.offlineSince) continue;
    const { seconds } = computeDuration(item.offlineSince, now);
    if (seconds > maxSeconds) {
      maxSeconds = seconds;
    }
  }
  return maxSeconds > 0 ? formatDurationShort(maxSeconds) : "-";
}

export function computeMostFlaps(items: Array<{ flapCount24h: number }>) {
  let maxFlaps = 0;
  for (const item of items) {
    if (item.flapCount24h > maxFlaps) {
      maxFlaps = item.flapCount24h;
    }
  }
  return maxFlaps;
}

export function formatDurationShort(totalSeconds: number) {
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;

  if (hours > 0) {
    return `${hours}h ${minutes.toString().padStart(2, "0")}m`;
  }
  if (minutes > 0) {
    return `${minutes}m ${seconds.toString().padStart(2, "0")}s`;
  }
  return `${seconds}s`;
}

export function formatDurationClock(totalSeconds: number) {
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;
  if (hours > 0) {
    return `${hours.toString().padStart(2, "0")}:${minutes.toString().padStart(2, "0")}:${seconds
      .toString()
      .padStart(2, "0")}`;
  }
  return `${minutes.toString().padStart(2, "0")}:${seconds.toString().padStart(2, "0")}`;
}

export function formatTimestamp(value?: string) {
  if (!value) return "-";
  const parsed = Date.parse(value);
  if (Number.isNaN(parsed)) return "-";
  return new Date(parsed).toLocaleString();
}
