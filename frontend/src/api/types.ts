export type DerivedState = "Ok" | "Unstable" | "Offline" | "Unknown";

export type Meta = {
  dataStale: boolean;
  sipassLastSuccessAt?: string;
  disconnectedSince?: string;
};

export type ObjectType = "Controller" | "AccessPoint";

export type ObjectEntity = {
  objectId: string;
  type: ObjectType | string | number;
  name: string;
  parentId?: string | null;
  path: string;
};

export type Overview = {
  counts: Record<string, number>;
  topOffline: StatusSnapshot[];
  topUnstable: StatusSnapshot[];
  snapshots?: StatusSnapshot[];
  objects?: ObjectEntity[];
  sipassConnection: { isConnected: boolean };
};

export type StatusSnapshot = {
  objectId: string;
  rawComms: boolean;
  derivedState: DerivedState | string | number;
  offlineSince?: string;
  lastOnlineAt?: string;
  lastSeenAt?: string;
  flapCount24h: number;
  totalOfflineSeconds24h: number;
};

export type EventLog = {
  id: number;
  timestamp: string;
  severity: string;
  objectId?: string | null;
  message: string;
  source: string;
  correlationId: string;
  userId?: string | null;
};

export type Envelope<T> = {
  meta: Meta;
  data: T;
};
