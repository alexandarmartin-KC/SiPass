import type { EventLog, Overview, StatusSnapshot } from "../api/types";

const nowIso = () => new Date().toISOString();

export function buildMockOverview(now: Date): { meta: { dataStale: boolean; sipassLastSuccessAt: string }; data: Overview } {
  const sipassLastSuccessAt = new Date(now.getTime() - 2 * 60 * 1000).toISOString();

  const objects = [
    {
      objectId: "CTRL-01",
      type: "Controller",
      name: "Main Entrance Controller",
      parentId: null,
      path: "Site A / Floor 1"
    },
    {
      objectId: "CTRL-02",
      type: "Controller",
      name: "Warehouse Controller",
      parentId: null,
      path: "Site A / Warehouse"
    },
    {
      objectId: "CTRL-03",
      type: "Controller",
      name: "Office Wing Controller",
      parentId: null,
      path: "Site B / Office Wing"
    }
  ];

  const accessPoints = buildAccessPoints();
  const snapshots = buildSnapshots(now, accessPoints);
  const counts = countStates(snapshots);

  return {
    meta: {
      dataStale: false,
      sipassLastSuccessAt
    },
    data: {
      counts,
      topOffline: snapshots.filter((item) => item.derivedState === "Offline").slice(0, 10),
      topUnstable: snapshots.filter((item) => item.derivedState === "Unstable").slice(0, 10),
      snapshots,
      objects: [...objects, ...accessPoints],
      sipassConnection: { isConnected: true }
    }
  };
}

export function buildMockEvents(): EventLog[] {
  return [
    {
      id: 1,
      timestamp: nowIso(),
      severity: "WARN",
      objectId: "CTRL-03",
      message: "Controller flap count exceeded threshold.",
      source: "StateEngine",
      correlationId: "mock-1",
      userId: null
    },
    {
      id: 2,
      timestamp: nowIso(),
      severity: "CRIT",
      objectId: "CTRL-02",
      message: "Controller offline beyond threshold.",
      source: "StateEngine",
      correlationId: "mock-2",
      userId: null
    }
  ];
}

function buildAccessPoints() {
  return [
    {
      objectId: "AP-01",
      type: "AccessPoint",
      name: "Main Entrance Door",
      parentId: "CTRL-01",
      path: "Site A / Floor 1 / Main Entrance"
    },
    {
      objectId: "AP-02",
      type: "AccessPoint",
      name: "Warehouse Door 1",
      parentId: "CTRL-02",
      path: "Site A / Warehouse / Door 1"
    },
    {
      objectId: "AP-03",
      type: "AccessPoint",
      name: "Warehouse Door 2",
      parentId: "CTRL-02",
      path: "Site A / Warehouse / Door 2"
    },
    {
      objectId: "AP-04",
      type: "AccessPoint",
      name: "Office Door 1",
      parentId: "CTRL-03",
      path: "Site B / Office Wing / Door 1"
    },
    {
      objectId: "AP-05",
      type: "AccessPoint",
      name: "Office Door 2",
      parentId: "CTRL-03",
      path: "Site B / Office Wing / Door 2"
    },
    {
      objectId: "AP-06",
      type: "AccessPoint",
      name: "Office Door 3",
      parentId: "CTRL-03",
      path: "Site B / Office Wing / Door 3"
    }
  ];
}

function buildSnapshots(now: Date, accessPoints: { objectId: string }[]): StatusSnapshot[] {
  const offlineSince = new Date(now.getTime() - 45 * 60 * 1000).toISOString();
  const unstableSince = new Date(now.getTime() - 10 * 60 * 1000).toISOString();
  const lastSeen = new Date(now.getTime() - 30 * 1000).toISOString();

  return [
    {
      objectId: "CTRL-01",
      rawComms: true,
      derivedState: "Ok",
      offlineSince: undefined,
      lastOnlineAt: lastSeen,
      lastSeenAt: lastSeen,
      flapCount24h: 0,
      totalOfflineSeconds24h: 0
    },
    {
      objectId: "CTRL-02",
      rawComms: false,
      derivedState: "Offline",
      offlineSince,
      lastOnlineAt: offlineSince,
      lastSeenAt: offlineSince,
      flapCount24h: 1,
      totalOfflineSeconds24h: 2700
    },
    {
      objectId: "CTRL-03",
      rawComms: true,
      derivedState: "Unstable",
      offlineSince: undefined,
      lastOnlineAt: unstableSince,
      lastSeenAt: lastSeen,
      flapCount24h: 12,
      totalOfflineSeconds24h: 600
    },
    {
      objectId: "AP-01",
      rawComms: true,
      derivedState: "Ok",
      offlineSince: undefined,
      lastOnlineAt: lastSeen,
      lastSeenAt: lastSeen,
      flapCount24h: 0,
      totalOfflineSeconds24h: 0
    },
    {
      objectId: "AP-02",
      rawComms: false,
      derivedState: "Offline",
      offlineSince,
      lastOnlineAt: offlineSince,
      lastSeenAt: offlineSince,
      flapCount24h: 2,
      totalOfflineSeconds24h: 2700
    },
    {
      objectId: "AP-03",
      rawComms: false,
      derivedState: "Offline",
      offlineSince,
      lastOnlineAt: offlineSince,
      lastSeenAt: offlineSince,
      flapCount24h: 2,
      totalOfflineSeconds24h: 2700
    },
    {
      objectId: "AP-04",
      rawComms: true,
      derivedState: "Unstable",
      offlineSince: undefined,
      lastOnlineAt: unstableSince,
      lastSeenAt: lastSeen,
      flapCount24h: 9,
      totalOfflineSeconds24h: 450
    },
    {
      objectId: "AP-05",
      rawComms: true,
      derivedState: "Ok",
      offlineSince: undefined,
      lastOnlineAt: lastSeen,
      lastSeenAt: lastSeen,
      flapCount24h: 0,
      totalOfflineSeconds24h: 0
    },
    {
      objectId: "AP-06",
      rawComms: true,
      derivedState: "Unstable",
      offlineSince: undefined,
      lastOnlineAt: unstableSince,
      lastSeenAt: lastSeen,
      flapCount24h: 6,
      totalOfflineSeconds24h: 300
    }
  ];
}

function countStates(items: StatusSnapshot[]) {
  return items.reduce<Record<string, number>>(
    (acc, item) => {
      const state = String(item.derivedState);
      acc[state] = (acc[state] ?? 0) + 1;
      return acc;
    },
    {}
  );
}
