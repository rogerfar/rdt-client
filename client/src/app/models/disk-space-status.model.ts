export interface DiskSpaceStatus {
  isPaused: boolean;
  availableSpaceGB: number;
  thresholdGB: number;
  lastCheckTime: Date;
}
