/**
 * Logging action types.
 * Server-sent properties are camelCase.
 */

export type LogLevel =
  | "Trace"
  | "Debug"
  | "Information"
  | "Warning"
  | "Error"
  | "Critical"
  | "None"
  | (string & {});

export const LogLevel = {
  Trace: "Trace" as LogLevel,
  Debug: "Debug" as LogLevel,
  Information: "Information" as LogLevel,
  Warning: "Warning" as LogLevel,
  Error: "Error" as LogLevel,
  Critical: "Critical" as LogLevel,
  None: "None" as LogLevel,
} as const;

export interface LogEntry {
  level: LogLevel;
  category: string;
  message: string;
  exception: unknown;
  /** ISO 8601 timestamp. */
  time: string;
}

export interface LogHistoryParams {
  /** Maximum entries to return (>= 1). */
  Max: number;
  /** ISO 8601 timestamp — return only entries before this time. */
  Before?: string | null;
  /** ISO 8601 timestamp — return only entries after this time. */
  After?: string | null;
  MinLevel: LogLevel;
}

export interface LogHistoryResult {
  isSuccess: boolean;
  failureReason: string | null;
  entries: LogEntry[] | null;
}
