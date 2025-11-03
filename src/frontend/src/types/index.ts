// API Types
export enum ConnectorType {
  AutodeskConstructionCloud = 0,
  SharePoint = 1,
}

export enum SyncDirection {
  OneWaySourceToTarget = 0,
  OneWayTargetToSource = 1,
  Bidirectional = 2,
}

export enum ConflictPolicy {
  SourceWins = 0,
  TargetWins = 1,
  LastWriterWins = 2,
  ManualHold = 3,
}

export enum SyncJobStatus {
  Pending = 0,
  Running = 1,
  Completed = 2,
  Failed = 3,
  Cancelled = 4,
  PartialSuccess = 5,
}

export interface Tenant {
  id: string;
  name: string;
  billingProfile?: string;
  createdAt: string;
  isActive: boolean;
}

export interface Connector {
  id: string;
  tenantId: string;
  type: ConnectorType;
  name: string;
  createdAt: string;
  isHealthy: boolean;
  healthMessage?: string;
  lastHealthCheck?: string;
}

export interface Binding {
  id: string;
  tenantId: string;
  sourceConnectorId: string;
  targetConnectorId: string;
  sourcePath: string;
  targetPath: string;
  direction: SyncDirection;
  conflictPolicy: ConflictPolicy;
  scheduleCadenceMinutes: number;
  isActive: boolean;
  isPaused: boolean;
  createdAt: string;
}

export interface SyncJob {
  id: string;
  bindingId: string;
  status: SyncJobStatus;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
  filesProcessed: number;
  bytesTransferred: number;
  conflictsDetected: number;
  retries: number;
  errorMessage?: string;
  isManualTrigger: boolean;
}

export interface SyncJobStats {
  totalJobs: number;
  completedJobs: number;
  failedJobs: number;
  runningJobs: number;
  totalBytesTransferred: number;
  totalFilesProcessed: number;
}
