import { computeDuration, formatDurationClock, formatTimestamp } from "../utils/overview";
import type { SseStatus } from "../hooks/useSse";

type Props = {
  dataStale: boolean;
  lastSuccessAt?: string;
  disconnectedSince?: string;
  sseStatus?: SseStatus;
  now: Date;
};

export default function ConnectionStrip({
  dataStale,
  lastSuccessAt,
  disconnectedSince,
  sseStatus,
  now
}: Props) {
  const statusLabel = dataStale ? "DATA STALE" : "LIVE";
  const statusTone = dataStale ? "danger" : "ok";
  const lastSyncLabel = formatTimestamp(lastSuccessAt);

  let disconnectedLabel: string | null = null;
  if (dataStale) {
    const sinceValue = disconnectedSince ?? lastSuccessAt;
    if (sinceValue) {
      const { seconds } = computeDuration(sinceValue, now);
      const approx = disconnectedSince ? "" : " (approx)";
      disconnectedLabel = `Disconnected for ${formatDurationClock(seconds)}${approx}`;
    } else {
      disconnectedLabel = "Disconnected for --:--";
    }
  }

  const streamLabel = sseStatus ? `Event stream: ${sseStatus}` : null;

  return (
    <section className="connection-strip" aria-live="polite">
      <div className={`strip-status strip-status--${statusTone}`}>{statusLabel}</div>
      <div className="strip-detail">Last sync: {lastSyncLabel}</div>
      {disconnectedLabel && <div className="strip-detail">{disconnectedLabel}</div>}
      {streamLabel && <div className="strip-detail">{streamLabel}</div>}
    </section>
  );
}
