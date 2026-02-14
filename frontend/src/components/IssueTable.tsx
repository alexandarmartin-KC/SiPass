import type { StatusSnapshot } from "../api/types";
import StatusPill from "./StatusPill";
import { normalizeDerivedState } from "../utils/overview";

type Props = {
  items: StatusSnapshot[];
};

export default function IssueTable({ items }: Props) {
  if (items.length === 0) {
    return <p className="subtle">No items to show.</p>;
  }

  return (
    <table className="table">
      <thead>
        <tr>
          <th>Object</th>
          <th>State</th>
          <th>Offline since</th>
          <th>Flaps (24h)</th>
        </tr>
      </thead>
      <tbody>
        {items.map((item) => (
          <tr key={item.objectId}>
            <td>{item.objectId}</td>
            <td>
              <StatusPill state={normalizeDerivedState(item.derivedState)} />
            </td>
            <td>{item.offlineSince ?? "-"}</td>
            <td>{item.flapCount24h}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
