/**
 * Action payload and result types.
 * Server-sent result properties are camelCase (JsonNamingPolicy.CamelCase).
 * Client-sent payload properties can be any case — server deserializes case-insensitively.
 */

import type { IMachineState, AtAGlanceMachineState, MachineMessage, MachineFileHandle, Material, SpoolLocation, MaterialToPrint } from "./state.js";

// ---------------------------------------------------------------------------
// StateDetails enum
// ---------------------------------------------------------------------------

export type StateDetails = "None" | "AtAGlance" | "Full";

export const StateDetails = {
  None: "None" as StateDetails,
  AtAGlance: "AtAGlance" as StateDetails,
  Full: "Full" as StateDetails,
} as const;

// ---------------------------------------------------------------------------
// Generic result types
// ---------------------------------------------------------------------------

export interface WebSocketClientActionResult {
  isSuccess: boolean;
  failureReason: string | null;
}

export interface ClientMessageMachineOperationResult {
  isSuccess: boolean;
  failureReason: string | null;
  failureMessage: MachineMessage | null;
}

// ---------------------------------------------------------------------------
// Machine subscription
// ---------------------------------------------------------------------------

export interface SubscribeToMachinePayload {
  MachineID: string;
  DetailOfState: StateDetails;
}

export interface UnsubscribeFromMachinePayload {
  MachineID: string;
}

export interface SubscribeActionResult {
  isSuccess: boolean;
  failureReason: string | null;
  /** Present when subscribed with StateDetails.Full. */
  fullState: IMachineState | null;
  /** Present when subscribed with StateDetails.AtAGlance. */
  atAGlanceState: AtAGlanceMachineState | null;
}

// ---------------------------------------------------------------------------
// Machine control actions
// ---------------------------------------------------------------------------

export interface MarkAsIdlePayload { MachineID: string }
export interface PauseMachinePayload { MachineID: string }
export interface ResumeMachinePayload { MachineID: string }
export interface StopMachinePayload { MachineID: string }
export interface ToggleLightMachinePayload {
  MachineID: string;
  FixtureName: string;
  IsOn: boolean;
}
export interface SetFanSpeedMachinePayload {
  MachineID: string;
  FanName: string;
  SpeedPercent: number;
}
export interface PrintOptions {
  CustomID?: string | null;
  LevelBed: boolean;
  FlowCalibration: boolean;
  VibrationCalibration: boolean;
  InspectFirstLayer: boolean;
  /** Extruder index → spool location. */
  MaterialMap?: Record<number, { MUID: string; Slot: number }> | null;
}
export interface StartPrintPayload {
  MachineID: string;
  /** The full file handle, as returned in LocalPrintJob.file. All four fields must match. */
  File: MachineFileHandle;
  Options: PrintOptions;
}

export interface SpoolMatch {
  location: SpoolLocation;
  materialInMatchedSpool: Material;
  deltaE: number;
}

export interface Matches<K extends string | number, V> {
  all: Record<K, V[]>;
  match: Record<K, V>;
  missing: K[];
  hasMissing: boolean;
}

export interface FindMatchingSpoolsPayload {
  MachineID: string;
  /** Extruder index → MaterialToPrint. */
  MaterialsToPrint: Record<number, MaterialToPrint>;
}

export interface FindMatchingSpoolsResult {
  matches: Matches<number, SpoolMatch>;
}

export interface ChangeMaterialPayload {
  MachineID: string;
  Location: SpoolLocation;
  Material: Material;
}

export interface SetPrintSpeedPayload {
  MachineID: string;
  /** 0–100 percent of the machine's speed range. */
  SpeedPercent: number;
}
