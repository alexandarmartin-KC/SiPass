import type { Envelope, EventLog, Overview } from "./types";

const apiBase = (import.meta.env.VITE_API_BASE as string | undefined) ?? "";
const baseUrl = apiBase.endsWith("/") ? apiBase.slice(0, -1) : apiBase;

function buildUrl(path: string) {
  if (path.startsWith("http")) {
    return path;
  }
  return `${baseUrl}${path}`;
}

async function getJson<T>(path: string): Promise<Envelope<T>> {
  const response = await fetch(buildUrl(path), { credentials: "include" });
  if (!response.ok) {
    throw new Error(`Request failed: ${response.status}`);
  }
  return response.json();
}

export function getOverview(): Promise<Envelope<Overview>> {
  return getJson<Overview>("/api/overview");
}

export function getEvents(): Promise<Envelope<EventLog[]>> {
  return getJson<EventLog[]>("/api/events");
}
