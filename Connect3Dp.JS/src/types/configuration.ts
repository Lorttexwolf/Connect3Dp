/**
 * Machine configuration types.
 * Mirrors Connect3DpWebSocketExtensions.Configuration.cs.
 */

export interface MachineConfigurationSummary {
  [machineId: string]: unknown;
}

export interface AllMachineConfigurationsResult {
  isSuccess: boolean;
  failureReason: string | null;
  configurations: MachineConfigurationSummary;
}
