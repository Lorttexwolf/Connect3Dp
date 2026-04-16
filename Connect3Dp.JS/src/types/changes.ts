/**
 * Change/diff types emitted by the server's source-generated partial builder system.
 * All property names are camelCase (server applies JsonNamingPolicy.CamelCase).
 *
 * Naming patterns (generated from C# property names via camelCase policy):
 *   Scalar field "foo":           fooHasChanged, fooPrevious, fooNew
 *   Dictionary field "bars":      barsAdded, barsRemoved, barsUpdated
 *   Set/HashSet field "bars":     barsAdded, barsRemoved
 *   Nested updater field "baz":   bazChanges (null when no change)
 *
 * Dictionary entries use C#'s KeyValuePair serialization: { key: ..., value: ... }
 */

import type {
  MachineStatus,
  MachineCapabilities,
  MachineAirDuctMode,
  MachineExtruder,
  MachineNozzle,
  HeatingElement,
  HeatingConstraints,
  HeatingJob,
  HeatingSchedule,
  MUnit,
  MUCapabilities,
  Spool,
  Material,
  MaterialColor,
  SpoolLocation,
  MachineMessage,
  Notification,
  HistoricPrintJob,
  LocalPrintJob,
  ScheduledPrint,
  MachineFileHandle,
} from "./state.js";

/** A C# KeyValuePair<K, V> serialized to JSON with camelCase naming policy. */
export interface KVP<K, V> {
  key: K;
  value: V;
}

// ---------------------------------------------------------------------------
// Leaf-level changes
// ---------------------------------------------------------------------------

export interface MaterialColorChanges {
  hasChanged: boolean;
  nameHasChanged: boolean;
  namePrevious: string | null;
  nameNew: string | null;
  rHasChanged: boolean;
  rPrevious: number | null;
  rNew: number | null;
  gHasChanged: boolean;
  gPrevious: number | null;
  gNew: number | null;
  bHasChanged: boolean;
  bPrevious: number | null;
  bNew: number | null;
}

export interface MaterialChanges {
  hasChanged: boolean;
  nameHasChanged: boolean;
  namePrevious: string | null;
  nameNew: string | null;
  colorChanges: MaterialColorChanges | null;
  fProfileIDXHasChanged: boolean;
  fProfileIDXPrevious: string | null;
  fProfileIDXNew: string | null;
}

export interface SpoolChanges {
  hasChanged: boolean;
  numberHasChanged: boolean;
  numberPrevious: number | null;
  numberNew: number | null;
  materialChanges: MaterialChanges | null;
  gramsMaximumHasChanged: boolean;
  gramsMaximumPrevious: number | null;
  gramsMaximumNew: number | null;
  gramsRemainingHasChanged: boolean;
  gramsRemainingPrevious: number | null;
  gramsRemainingNew: number | null;
}

export interface MachineExtruderChanges {
  hasChanged: boolean;
  numberHasChanged: boolean;
  numberPrevious: number | null;
  numberNew: number | null;
  heatingConstraintHasChanged: boolean;
  heatingConstraintPrevious: HeatingConstraints | null;
  heatingConstraintNew: HeatingConstraints | null;
  tempCHasChanged: boolean;
  tempCPrevious: number | null;
  tempCNew: number | null;
  targetTempCHasChanged: boolean;
  targetTempCPrevious: number | null;
  targetTempCNew: number | null;
  nozzleNumberHasChanged: boolean;
  nozzleNumberPrevious: number | null;
  nozzleNumberNew: number | null;
  loadedSpoolHasChanged: boolean;
  loadedSpoolPrevious: SpoolLocation | null;
  loadedSpoolNew: SpoolLocation | null;
}

export interface MachineNozzleChanges {
  hasChanged: boolean;
  numberHasChanged: boolean;
  numberPrevious: number | null;
  numberNew: number | null;
  diameterHasChanged: boolean;
  diameterPrevious: number | null;
  diameterNew: number | null;
}

export interface NotificationChanges {
  hasChanged: boolean;
  messageHasChanged: boolean;
  messagePrevious: MachineMessage | null;
  messageNew: MachineMessage | null;
  lastSeenAtHasChanged: boolean;
  lastSeenAtPrevious: string | null;
  lastSeenAtNew: string | null;
}

export interface MUnitChanges {
  hasChanged: boolean;
  iDHasChanged: boolean;
  iDPrevious: string | null;
  iDNew: string | null;
  capacityHasChanged: boolean;
  capacityPrevious: number | null;
  capacityNew: number | null;
  modelHasChanged: boolean;
  modelPrevious: string | null;
  modelNew: string | null;
  capabilitiesHasChanged: boolean;
  capabilitiesPrevious: MUCapabilities | null;
  capabilitiesNew: MUCapabilities | null;
  heatingConstraintsHasChanged: boolean;
  heatingConstraintsPrevious: HeatingConstraints | null;
  heatingConstraintsNew: HeatingConstraints | null;
  traysAdded: Array<KVP<number, Spool>>;
  traysRemoved: number[];
  traysUpdated: Array<KVP<number, SpoolChanges>>;
  humidityPercentHasChanged: boolean;
  humidityPercentPrevious: number | null;
  humidityPercentNew: number | null;
  temperatureCHasChanged: boolean;
  temperatureCPrevious: number | null;
  temperatureCNew: number | null;
  heatingJobHasChanged: boolean;
  heatingJobPrevious: HeatingJob | null;
  heatingJobNew: HeatingJob | null;
  heatingScheduleAdded: HeatingSchedule[];
  heatingScheduleRemoved: HeatingSchedule[];
}

// ---------------------------------------------------------------------------
// PrintJobChanges
// ---------------------------------------------------------------------------

export interface PrintJobChanges {
  hasChanged: boolean;
  nameHasChanged: boolean;
  namePrevious: string | null;
  nameNew: string | null;
  customIDHasChanged: boolean;
  customIDPrevious: string | null;
  customIDNew: string | null;
  percentageCompleteHasChanged: boolean;
  percentageCompletePrevious: number | null;
  percentageCompleteNew: number | null;
  remainingTimeHasChanged: boolean;
  remainingTimePrevious: string | null;
  remainingTimeNew: string | null;
  totalTimeHasChanged: boolean;
  totalTimePrevious: string | null;
  totalTimeNew: string | null;
  issueHasChanged: boolean;
  issuePrevious: MachineMessage | null;
  issueNew: MachineMessage | null;
  thumbnailHasChanged: boolean;
  thumbnailPrevious: MachineFileHandle | null;
  thumbnailNew: MachineFileHandle | null;
  fileHasChanged: boolean;
  filePrevious: MachineFileHandle | null;
  fileNew: MachineFileHandle | null;
  subStageHasChanged: boolean;
  subStagePrevious: string | null;
  subStageNew: string | null;
  totalMaterialUsageHasChanged: boolean;
  totalMaterialUsagePrevious: number | null;
  totalMaterialUsageNew: number | null;
  localPathHasChanged: boolean;
  localPathPrevious: string | null;
  localPathNew: string | null;
  /** Key format: "{MUID}:{Slot}" */
  spoolMaterialUsagesAdded: Array<KVP<string, number>>;
  spoolMaterialUsagesRemoved: string[];
  spoolMaterialUsagesUpdated: Array<KVP<string, number>>;
}

// ---------------------------------------------------------------------------
// MachineStateChanges — top-level diff
// ---------------------------------------------------------------------------

export interface MachineStateChanges {
  hasChanged: boolean;
  brandHasChanged: boolean;
  brandPrevious: string | null;
  brandNew: string | null;
  modelHasChanged: boolean;
  modelPrevious: string | null;
  modelNew: string | null;
  nicknameHasChanged: boolean;
  nicknamePrevious: string | null;
  nicknameNew: string | null;
  capabilitiesHasChanged: boolean;
  capabilitiesPrevious: MachineCapabilities | null;
  capabilitiesNew: MachineCapabilities | null;
  statusHasChanged: boolean;
  statusPrevious: MachineStatus | null;
  statusNew: MachineStatus | null;
  /** Null when CurrentJob has not changed. */
  currentJobChanges: PrintJobChanges | null;
  jobHistoryAdded: HistoricPrintJob[];
  jobHistoryRemoved: HistoricPrintJob[];
  localJobsAdded: LocalPrintJob[];
  localJobsRemoved: LocalPrintJob[];
  scheduledPrintsAdded: ScheduledPrint[];
  scheduledPrintsRemoved: ScheduledPrint[];
  extrudersAdded: Array<KVP<number, MachineExtruder>>;
  extrudersRemoved: number[];
  extrudersUpdated: Array<KVP<number, MachineExtruderChanges>>;
  nozzlesAdded: Array<KVP<number, MachineNozzle>>;
  nozzlesRemoved: number[];
  nozzlesUpdated: Array<KVP<number, MachineNozzleChanges>>;
  materialUnitsAdded: Array<KVP<string, MUnit>>;
  materialUnitsRemoved: string[];
  materialUnitsUpdated: Array<KVP<string, MUnitChanges>>;
  airDuctModeHasChanged: boolean;
  airDuctModePrevious: MachineAirDuctMode | null;
  airDuctModeNew: MachineAirDuctMode | null;
  fansAdded: Array<KVP<string, number>>;
  fansRemoved: string[];
  /** Full new value for each updated fan. */
  fansUpdated: Array<KVP<string, number>>;
  lightsAdded: Array<KVP<string, boolean>>;
  lightsRemoved: string[];
  /** Full new value for each updated light. */
  lightsUpdated: Array<KVP<string, boolean>>;
  /** Full new HeatingElement for each addition. */
  heatingElementsAdded: Array<KVP<string, HeatingElement>>;
  heatingElementsRemoved: string[];
  /** Full new HeatingElement for each update (HeatingElement has no sub-updater). */
  heatingElementsUpdated: Array<KVP<string, HeatingElement>>;
  isLocalStorageScanningHasChanged: boolean;
  isLocalStorageScanningPrevious: boolean | null;
  isLocalStorageScanningNew: boolean | null;
  streamingOMEURLHasChanged: boolean;
  streamingOMEURLPrevious: string | null;
  streamingOMEURLNew: string | null;
  thumbnailOMEURLHasChanged: boolean;
  thumbnailOMEURLPrevious: string | null;
  thumbnailOMEURLNew: string | null;
  notificationsAdded: Array<KVP<string, Notification>>;
  notificationsRemoved: string[];
  notificationsUpdated: Array<KVP<string, NotificationChanges>>;
}

// ---------------------------------------------------------------------------
// AtAGlanceMachineStateChanges
// ---------------------------------------------------------------------------

export interface AtAGlanceMachineStateChanges {
  hasChanged: boolean;
  statusHasChanged: boolean;
  statusPrevious: MachineStatus | null;
  statusNew: MachineStatus | null;
  capabilitiesHasChanged: boolean;
  capabilitiesPrevious: MachineCapabilities | null;
  capabilitiesNew: MachineCapabilities | null;
  nicknameHasChanged: boolean;
  nicknamePrevious: string | null;
  nicknameNew: string | null;
  currentJobChanges: PrintJobChanges | null;
}

// ---------------------------------------------------------------------------
// Broadcast payload
// ---------------------------------------------------------------------------

export interface BroadcastedMachineStateUpdateData {
  /** Populated when subscribed with StateDetails.Full; null otherwise. */
  fullChanges: MachineStateChanges | null;
  /** Populated when subscribed with StateDetails.AtAGlance; null otherwise. */
  atAGlanceChanges: AtAGlanceMachineStateChanges | null;
}
