// Client
export { Connect3DpClient } from "./client.js";
export type {
  Connect3DpClientOptions,
  Connect3DpEventMap,
  ReconnectOptions,
} from "./client.js";

// Common protocol types
export type { MessageToClient, MessageToServer } from "./types/common.js";
export { Topics } from "./types/common.js";

// State model
export type {
  IMachineState,
  AtAGlanceMachineState,
  MachineStatus,
  MachineCapabilities,
  MachineAirDuctMode,
  MachineExtruder,
  MachineNozzle,
  HeatingElement,
  HeatingConstraints,
  HeatingJob,
  HeatingSettings,
  HeatingSchedule,
  MUnit,
  MUCapabilities,
  Spool,
  SpoolLocation,
  Material,
  MaterialColor,
  MaterialToPrint,
  PrintOptions,
  PrintJob,
  HistoricPrintJob,
  LocalPrintJob,
  ScheduledPrint,
  MachineMessage,
  MachineMessageSeverity,
  MachineMessageActions,
  MachineMessageAutoResolve,
  Notification,
  MachineFileHandle,
  CameraSpec,
  CameraStream,
  MachineStreamingURLs,
} from "./types/state.js";
export { MachineStatusFlags, MachineCapabilityFlags, MUCapabilityFlags, hasCapability } from "./types/state.js";

// Changes / diffs
export type {
  BroadcastedMachineStateUpdateData,
  MachineStateChanges,
  AtAGlanceMachineStateChanges,
  PrintJobChanges,
  MachineExtruderChanges,
  MachineNozzleChanges,
  MUnitChanges,
  SpoolChanges,
  MaterialChanges,
  MaterialColorChanges,
  NotificationChanges,
  KVP,
} from "./types/changes.js";

// Actions
export type {
  SubscribeActionResult,
  WebSocketClientActionResult,
  ClientMessageMachineOperationResult,
  SubscribeToMachinePayload,
  UnsubscribeFromMachinePayload,
  MarkAsIdlePayload,
  PauseMachinePayload,
  ResumeMachinePayload,
  StopMachinePayload,
  ToggleLightMachinePayload,
  SetFanSpeedMachinePayload,
  StartPrintPayload,
  PrintOptions as StartPrintOptions,
  SpoolMatch,
  Matches,
  FindMatchingSpoolsPayload,
  FindMatchingSpoolsResult,
  ChangeMaterialPayload,
} from "./types/actions.js";
export { StateDetails } from "./types/actions.js";

// Logging
export type {
  LogEntry,
  LogHistoryParams,
  LogHistoryResult,
} from "./types/logging.js";
export { LogLevel } from "./types/logging.js";

// File store
export type {
  StorageInfo,
  MachineFileStoreTotalUsageResult,
  MachineFileStoreMachineUsageResult,
} from "./types/file-store.js";

// Machine discovery / configuration list
export type {
  MachineConfigurationSummary,
  AllMachineConfigurationsResult,
} from "./types/configuration.js";
