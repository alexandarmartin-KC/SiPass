import { useEffect, useState } from "react";
import type { StatusSnapshot } from "../api/types";
import IssueTable from "../components/IssueTable";

export default function Controllers() {
  const [items, setItems] = useState<StatusSnapshot[]>([]);

  useEffect(() => {
    // TODO: wire /api/controllers to include status summaries.
    setItems([]);
  }, []);

  return (
    <section className="panel">
      <h2>Controllers</h2>
      <IssueTable items={items} />
    </section>
  );
}
