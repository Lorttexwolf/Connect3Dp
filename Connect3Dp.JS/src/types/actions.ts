/**
 * Action payload and result types.
 * Server-sent result properties are camelCase (JsonNamingPolicy.CamelCase).
 * Client-sent payload properties can be any case — server deserializes case-insensitively.
 */

import type { IMachineState, AtAGlanceMachineState, MachineMessage } from "./state.js";

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
