export type MachineStatus =
  | "Disconnected"
  | "Connecting"
  | "Idle"
  | "Printing"
  | "Printed"
  | "Paused"
  | "Canceled"
  | (string & {});

export const MachineStatusFlags = {
  Disconnected: 0,
  Connecting: 1,
  Idle: 2,
  Printing: 4,
  Printed: 8,
  Paused: 16,
  Canceled: 32,
} as const;

/** Comma-separated flag names, e.g. "StartLocalJob, Control, Camera". */
export type MachineCapabilities = string;

export const MachineCapabilityFlags = {
  None: "None",
  StartLocalJob: "StartLocalJob",
  Control: "Control",
  Camera: "Camera",
  Lighting: "Lighting",
  PrintHistory: "PrintHistory",
  LocalJobs: "LocalJobs",
  Temps: "Temps",
  Fans: "Fans",
  Nozzles: "Nozzles",
  AirDuct: "AirDuct",
  Print_Options_BedLevel: "Print_Options_BedLevel",
  Print_Options_FlowCalibration: "Print_Options_FlowCalibration",
  Print_Options_VibrationCalibration: "Print_Options_VibrationCalibration",
  Print_Options_InspectFirstLayer: "Print_Options_InspectFirstLayer",
} as const;

/** Returns true when the machine's capabilities string includes the given flag. */
export function hasCapability(capabilities: MachineCapabilities, flag: string): boolean {
  return capabilities.split(", ").includes(flag);
}

export type MachineAirDuctMode = "None" | "Cooling" | "Heating" | (string & {});

export type MachineMessageSeverity = "Info" | "Success" | "Warning" | "Error" | (string & {});

/** Comma-separated flags, e.g. "Resume, Cancel". */
export type MachineMessageActions = string;

/** Comma-separated flags, e.g. "AutomaticFeeding, Heating, Humidity". */
export type MUCapabilities = string;

export const MUCapabilityFlags = {
  None: "None",
  AutomaticFeeding: "AutomaticFeeding",
  Heating: "Heating",
  Heating_CanSpin: "Heating_CanSpin",
  Humidity: "Humidity",
  Temperature: "Temperature",
  ModifyTray: "ModifyTray",
  ModifyTray_CannotMaxGrams: "ModifyTray_CannotMaxGrams",
} as const;

export interface HeatingConstraints {
  minTempC: number;
  maxTempC: number;
}

export interface HeatingElement {
  tempC: number;
  targetTempC: number;
  constraints: HeatingConstraints;
}

export interface MaterialColor {
  name?: string | null;
  r: number;
  g: number;
  b: number;
}

export interface Material {
  name: string;
  color: MaterialColor;
  fProfileIDX?: string | null;
}

export interface SpoolLocation {
  /** JSON key is "muid" (via [JsonPropertyName]). */
  muid: string;
  slot: number;
}

export interface Spool {
  number: number;
  material: Material;
  gramsMaximum?: number | null;
  gramsRemaining?: number | null;
}

export interface MachineExtruder {
  number: number;
  heatingConstraint: HeatingConstraints;
  tempC: number;
  targetTempC?: number | null;
  nozzleNumber?: number | null;
  loadedSpool?: SpoolLocation | null;
}

export interface MachineNozzle {
  number: number;
  diameter: number;
}

export interface MachineMessageAutoResolve {
  whenConnected?: boolean | null;
  whenStatus?: MachineStatus | null;
  whenPrinting?: boolean | null;
}

export interface MachineMessage {
  id: string;
  title: string;
  body: string;
  severity: MachineMessageSeverity;
  manualResolve: MachineMessageActions;
  autoResolve: MachineMessageAutoResolve;
}

export interface Notification {
  message: MachineMessage;
  /** ISO 8601 timestamp. */
  issuedAt: string;
  /** ISO 8601 timestamp. */
  lastSeenAt: string;
}

export interface HeatingJob {
  tempC: number;
  /** ISO 8601 duration, e.g. "PT1H30M". */
  duration: string;
}

export interface HeatingSettings {
  tempC: number;
  /** ISO 8601 duration. */
  duration: string;
  doSpin?: boolean | null;
}

export interface HeatingSchedule {
  timing: string;
  settings: HeatingSettings;
}

// ---------------------------------------------------------------------------
// Material unit (AMS)
// ---------------------------------------------------------------------------

export interface MUnit {
  id: string;
  capacity: number;
  model?: string | null;
  capabilities: MUCapabilities;
  heatingConstraints?: HeatingConstraints | null;
  /** Spools keyed by tray slot number. */
  trays: Record<number, Spool>;
  humidityPercent?: number | null;
  temperatureC?: number | null;
  heatingJob?: HeatingJob | null;
  heatingSchedule: HeatingSchedule[];
}

// ---------------------------------------------------------------------------
// File handles
// ---------------------------------------------------------------------------

export interface MachineFileHandle {
  machineID: string;
  /** JSON key is "uri" (via [JsonPropertyName]). */
  uri: string;
  /** JSON key is "mime" (via [JsonPropertyName]). */
  mime: string;
  /** JSON key is "hashSha256" (via [JsonPropertyName]). */
  hashSha256: string;
}

// ---------------------------------------------------------------------------
// Print jobs
// ---------------------------------------------------------------------------

export interface MaterialToPrint {
  material: Material;
  totalGramsUsed: number;
  nozzleDiameter: number;
}

export interface PrintOptions {
  customID?: string | null;
  levelBed: boolean;
  flowCalibration: boolean;
  vibrationCalibration: boolean;
  inspectFirstLayer: boolean;
  /** Extruder index → spool location. */
  materialMap?: Record<number, SpoolLocation> | null;
}

export interface PrintJob {
  name: string;
  customID?: string | null;
  /** 0–100 */
  percentageComplete: number;
  /** ISO 8601 duration, e.g. "PT45M". */
  remainingTime: string;
  /** ISO 8601 duration. */
  totalTime: string;
  issue?: MachineMessage | null;
  thumbnail?: MachineFileHandle | null;
  file?: MachineFileHandle | null;
  subStage?: string | null;
  totalMaterialUsage?: number | null;
  localPath?: string | null;
  /**
   * Material usage per spool. Keys are "{MUID}:{Slot}" (from SpoolLocation.ToString()).
   * Values are grams used.
   */
  spoolMaterialUsages?: Record<string, number> | null;
}

export interface HistoricPrintJob {
  name: string;
  isSuccess: boolean;
  /** ISO 8601 datetime. */
  endedAt: string;
  /** ISO 8601 duration. */
  elapsed: string;
  thumbnail?: MachineFileHandle | null;
  file?: MachineFileHandle | null;
}

export interface LocalPrintJob {
  name: string;
  file: MachineFileHandle;
  totalGramsUsed: number;
  /** ISO 8601 duration. */
  time: string;
  /** Extruder index → material. */
  materialsToPrint: Record<number, MaterialToPrint>;
}

export interface ScheduledPrint {
  timing: string;
  localJob: LocalPrintJob;
  options: PrintOptions;
  /** "{MUID}:{Slot}" → material. */
  initialMapping?: Record<string, Material>;
}

// ---------------------------------------------------------------------------
// Camera streaming
// ---------------------------------------------------------------------------

export interface CameraSpec {
  width: number | null;
  height: number | null;
  fps: number | null;
}

export interface CameraStream {
  url: string;
  spec: CameraSpec | null;
}

export interface MachineStreamingURLs {
  glance: CameraStream;
  full: CameraStream;
}

// ---------------------------------------------------------------------------
// Full machine state
// ---------------------------------------------------------------------------

export interface IMachineState {
  brand?: string | null;
  model?: string | null;
  nickname?: string | null;
  capabilities: MachineCapabilities;
  status: MachineStatus;
  currentJob?: PrintJob | null;
  jobHistory: HistoricPrintJob[];
  localJobs: LocalPrintJob[];
  scheduledPrints: ScheduledPrint[];
  /** Extruders keyed by extruder number. */
  extruders: Record<number, MachineExtruder>;
  /** Nozzles keyed by nozzle number. */
  nozzles: Record<number, MachineNozzle>;
  /** Material units (AMS) keyed by unit ID. */
  materialUnits: Record<string, MUnit>;
  airDuctMode: MachineAirDuctMode;
  /** Fan speeds keyed by fan name. */
  fans: Record<string, number>;
  /** Light states keyed by light name. */
  lights: Record<string, boolean>;
  /** Heating elements (bed, chamber, etc.) keyed by name. */
  heatingElements: Record<string, HeatingElement>;
  isLocalStorageScanning: boolean;
  streamingURLs?: MachineStreamingURLs | null;
  /** Notifications keyed by message ID. */
  notifications: Record<string, Notification>;
}

// ---------------------------------------------------------------------------
// AtAGlance state (lightweight subset)
// ---------------------------------------------------------------------------

/** Lightweight job summary as exposed by IMachinePrintJob. */
export interface AtAGlanceJob {
  name: string;
  customID?: string | null;
  percentageComplete: number;
  remainingTime: string;
  totalTime: string;
  issue?: MachineMessage | null;
  file?: MachineFileHandle | null;
  thumbnail?: MachineFileHandle | null;
  subStage?: string | null;
  totalMaterialUsage?: number | null;
}

export interface AtAGlanceMachineState {
  status: MachineStatus;
  capabilities: MachineCapabilities;
  nickname?: string | null;
  currentJob?: AtAGlanceJob | null;
}
