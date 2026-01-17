export interface RateLimitStatus {
  nextDequeueTime: Date | null;
  secondsRemaining: number;
}
