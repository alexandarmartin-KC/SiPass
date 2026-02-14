import type { DerivedState } from "../api/types";

const colors: Record<DerivedState, string> = {
  Ok: "#136a5f",
  Unstable: "#b15c16",
  Offline: "#8d1f2a",
  Unknown: "#5d6a70"
};

export default function StatusPill({ state }: { state: DerivedState }) {
  return (
    <span
      style={{
        display: "inline-flex",
        padding: "4px 10px",
        borderRadius: 999,
        background: colors[state] + "22",
        color: colors[state],
        fontSize: 12,
        fontWeight: 600
      }}
    >
      {state}
    </span>
  );
}
