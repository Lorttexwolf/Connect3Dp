export interface StorageInfo {
  totalBytes: number;
  fileCount: number;
  oldestFileDate: string | null;
  newestFileDate: string | null;
}

export interface MachineFileStoreTotalUsageResult {
  isSuccess: boolean;
  failureReason: string | null;
  usage: StorageInfo | null;
}

export interface MachineFileStoreMachineUsageResult {
  isSuccess: boolean;
  failureReason: string | null;
  machineID: string | null;
  usage: StorageInfo | null;
}
