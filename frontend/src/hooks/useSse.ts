import { useEffect, useRef, useState } from "react";

export type SseEvent = {
  event: string;
  data: unknown;
};

export type SseStatus = "connecting" | "connected" | "disconnected";

const apiBase = (import.meta.env.VITE_API_BASE as string | undefined) ?? "";
const baseUrl = apiBase.endsWith("/") ? apiBase.slice(0, -1) : apiBase;

export default function useSse() {
  const [lastEvent, setLastEvent] = useState<SseEvent | null>(null);
  const [status, setStatus] = useState<SseStatus>("connecting");
  const sourceRef = useRef<EventSource | null>(null);

  useEffect(() => {
    const source = new EventSource(`${baseUrl}/api/stream`, { withCredentials: true });
    sourceRef.current = source;

    source.onopen = () => {
      setStatus("connected");
    };

    source.addEventListener("statusChanged", (evt) => {
      const data = JSON.parse((evt as MessageEvent).data);
      setLastEvent({ event: "statusChanged", data });
    });

    source.addEventListener("sipassConnectionChanged", (evt) => {
      const data = JSON.parse((evt as MessageEvent).data);
      setLastEvent({ event: "sipassConnectionChanged", data });
    });

    source.onerror = () => {
      setStatus("disconnected");
      setLastEvent({ event: "sseError", data: null });
    };

    return () => {
      source.close();
    };
  }, []);

  return { lastEvent, status };
}
