#nullable enable
namespace Lib3Dp.State;

public readonly record struct MachineStateChanges(
	bool IDHasChanged,
	string? IDPrevious,
	string? IDNew,
	bool BrandHasChanged,
	string? BrandPrevious,
	string? BrandNew,
	bool ModelHasChanged,
	string? ModelPrevious,
	string? ModelNew,
	bool NicknameHasChanged,
	string? NicknamePrevious,
	string? NicknameNew,
	bool CapabilitiesHasChanged,
	Lib3Dp.State.MachineCapabilities? CapabilitiesPrevious,
	Lib3Dp.State.MachineCapabilities? CapabilitiesNew,
	bool StatusHasChanged,
	Lib3Dp.State.MachineStatus? StatusPrevious,
	Lib3Dp.State.MachineStatus? StatusNew,
	PrintJobChanges? CurrentJobChanges,
	Lib3Dp.State.HistoricPrintJob[] JobHistoryAdded,
	Lib3Dp.State.HistoricPrintJob[] JobHistoryRemoved,
	Lib3Dp.State.LocalPrintJob[] LocalJobsAdded,
	Lib3Dp.State.LocalPrintJob[] LocalJobsRemoved,
	Lib3Dp.State.ScheduledPrint[] ScheduledPrintsAdded,
	Lib3Dp.State.ScheduledPrint[] ScheduledPrintsRemoved,
	KeyValuePair<int, Lib3Dp.State.MachineExtruder>[] ExtrudersAdded,
	int[] ExtrudersRemoved,
	KeyValuePair<int, MachineExtruderChanges>[] ExtrudersUpdated,
	KeyValuePair<int, Lib3Dp.State.MachineNozzle>[] NozzlesAdded,
	int[] NozzlesRemoved,
	KeyValuePair<int, MachineNozzleChanges>[] NozzlesUpdated,
	KeyValuePair<string, Lib3Dp.State.MUnit>[] MaterialUnitsAdded,
	string[] MaterialUnitsRemoved,
	KeyValuePair<string, MUnitChanges>[] MaterialUnitsUpdated,
	bool AirDuctModeHasChanged,
	Lib3Dp.State.MachineAirDuctMode? AirDuctModePrevious,
	Lib3Dp.State.MachineAirDuctMode? AirDuctModeNew,
	KeyValuePair<string, int>[] FansAdded,
	string[] FansRemoved,
	KeyValuePair<string, int>[] FansUpdated,
	KeyValuePair<string, bool>[] LightsAdded,
	string[] LightsRemoved,
	KeyValuePair<string, bool>[] LightsUpdated,
	KeyValuePair<string, Lib3Dp.State.HeatingElement>[] HeatingElementsAdded,
	string[] HeatingElementsRemoved,
	KeyValuePair<string, Lib3Dp.State.HeatingElement>[] HeatingElementsUpdated,
	bool StreamingOMEURLHasChanged,
	string? StreamingOMEURLPrevious,
	string? StreamingOMEURLNew,
	bool ThumbnailOMEURLHasChanged,
	string? ThumbnailOMEURLPrevious,
	string? ThumbnailOMEURLNew,
	KeyValuePair<Lib3Dp.State.MachineMessage, Lib3Dp.State.Notification>[] NotificationsAdded,
	Lib3Dp.State.MachineMessage[] NotificationsRemoved,
	KeyValuePair<Lib3Dp.State.MachineMessage, NotificationChanges>[] NotificationsUpdated
)
{
    public bool HasChanged => IDHasChanged || BrandHasChanged || ModelHasChanged || NicknameHasChanged || CapabilitiesHasChanged || StatusHasChanged || CurrentJobChanges?.HasChanged == true || JobHistoryAdded?.Length > 0 || JobHistoryRemoved?.Length > 0 || LocalJobsAdded?.Length > 0 || LocalJobsRemoved?.Length > 0 || ScheduledPrintsAdded?.Length > 0 || ScheduledPrintsRemoved?.Length > 0 || ExtrudersAdded?.Length > 0 || ExtrudersRemoved?.Length > 0 || ExtrudersUpdated?.Length > 0 || NozzlesAdded?.Length > 0 || NozzlesRemoved?.Length > 0 || NozzlesUpdated?.Length > 0 || MaterialUnitsAdded?.Length > 0 || MaterialUnitsRemoved?.Length > 0 || MaterialUnitsUpdated?.Length > 0 || AirDuctModeHasChanged || FansAdded?.Length > 0 || FansRemoved?.Length > 0 || FansUpdated?.Length > 0 || LightsAdded?.Length > 0 || LightsRemoved?.Length > 0 || LightsUpdated?.Length > 0 || HeatingElementsAdded?.Length > 0 || HeatingElementsRemoved?.Length > 0 || HeatingElementsUpdated?.Length > 0 || StreamingOMEURLHasChanged || ThumbnailOMEURLHasChanged || NotificationsAdded?.Length > 0 || NotificationsRemoved?.Length > 0 || NotificationsUpdated?.Length > 0;

	public override string ToString()
	{
		if (!HasChanged) return $"{nameof(MachineStateChanges)}";
		var parts = new List<string>();
		if (IDHasChanged) 
			parts.Add($"ID = Previous: {IDPrevious}, New: {IDNew}");
		if (BrandHasChanged) 
			parts.Add($"Brand = Previous: {BrandPrevious}, New: {BrandNew}");
		if (ModelHasChanged) 
			parts.Add($"Model = Previous: {ModelPrevious}, New: {ModelNew}");
		if (NicknameHasChanged) 
			parts.Add($"Nickname = Previous: {NicknamePrevious}, New: {NicknameNew}");
		if (CapabilitiesHasChanged) 
			parts.Add($"Capabilities = Previous: {CapabilitiesPrevious}, New: {CapabilitiesNew}");
		if (StatusHasChanged) 
			parts.Add($"Status = Previous: {StatusPrevious}, New: {StatusNew}");
		if (CurrentJobChanges?.HasChanged == true)
			parts.Add($"CurrentJob = {CurrentJobChanges}");
		if (JobHistoryAdded?.Length > 0)
			parts.Add($"JobHistoryAdded = [{(string.Join(", ", JobHistoryAdded.Select(e => e.ToString())))}]");

		if (JobHistoryRemoved?.Length > 0)
			parts.Add($"JobHistoryRemoved = [{(string.Join(", ", JobHistoryRemoved.Select(e => e.ToString())))}]");
		if (LocalJobsAdded?.Length > 0)
			parts.Add($"LocalJobsAdded = [{(string.Join(", ", LocalJobsAdded.Select(e => e.ToString())))}]");

		if (LocalJobsRemoved?.Length > 0)
			parts.Add($"LocalJobsRemoved = [{(string.Join(", ", LocalJobsRemoved.Select(e => e.ToString())))}]");
		if (ScheduledPrintsAdded?.Length > 0)
			parts.Add($"ScheduledPrintsAdded = [{(string.Join(", ", ScheduledPrintsAdded.Select(e => e.ToString())))}]");

		if (ScheduledPrintsRemoved?.Length > 0)
			parts.Add($"ScheduledPrintsRemoved = [{(string.Join(", ", ScheduledPrintsRemoved.Select(e => e.ToString())))}]");
		if (ExtrudersAdded?.Length > 0)
			parts.Add($"ExtrudersAdded = [{(string.Join(", ", ExtrudersAdded.Select(e => e.ToString())))}]");

		if (ExtrudersRemoved?.Length > 0)
			parts.Add($"ExtrudersRemoved = [{(string.Join(", ", ExtrudersRemoved.Select(e => e.ToString())))}]");

		if (ExtrudersUpdated?.Length > 0)
			parts.Add($"ExtrudersUpdated = [{(string.Join(", ", ExtrudersUpdated.Select(e => e.ToString())))}]");
		if (NozzlesAdded?.Length > 0)
			parts.Add($"NozzlesAdded = [{(string.Join(", ", NozzlesAdded.Select(e => e.ToString())))}]");

		if (NozzlesRemoved?.Length > 0)
			parts.Add($"NozzlesRemoved = [{(string.Join(", ", NozzlesRemoved.Select(e => e.ToString())))}]");

		if (NozzlesUpdated?.Length > 0)
			parts.Add($"NozzlesUpdated = [{(string.Join(", ", NozzlesUpdated.Select(e => e.ToString())))}]");
		if (MaterialUnitsAdded?.Length > 0)
			parts.Add($"MaterialUnitsAdded = [{(string.Join(", ", MaterialUnitsAdded.Select(e => e.ToString())))}]");

		if (MaterialUnitsRemoved?.Length > 0)
			parts.Add($"MaterialUnitsRemoved = [{(string.Join(", ", MaterialUnitsRemoved.Select(e => e.ToString())))}]");

		if (MaterialUnitsUpdated?.Length > 0)
			parts.Add($"MaterialUnitsUpdated = [{(string.Join(", ", MaterialUnitsUpdated.Select(e => e.ToString())))}]");
		if (AirDuctModeHasChanged) 
			parts.Add($"AirDuctMode = Previous: {AirDuctModePrevious}, New: {AirDuctModeNew}");
		if (FansAdded?.Length > 0)
			parts.Add($"FansAdded = [{(string.Join(", ", FansAdded.Select(e => e.ToString())))}]");

		if (FansRemoved?.Length > 0)
			parts.Add($"FansRemoved = [{(string.Join(", ", FansRemoved.Select(e => e.ToString())))}]");

		if (FansUpdated?.Length > 0)
			parts.Add($"FansUpdated = [{(string.Join(", ", FansUpdated.Select(e => e.ToString())))}]");
		if (LightsAdded?.Length > 0)
			parts.Add($"LightsAdded = [{(string.Join(", ", LightsAdded.Select(e => e.ToString())))}]");

		if (LightsRemoved?.Length > 0)
			parts.Add($"LightsRemoved = [{(string.Join(", ", LightsRemoved.Select(e => e.ToString())))}]");

		if (LightsUpdated?.Length > 0)
			parts.Add($"LightsUpdated = [{(string.Join(", ", LightsUpdated.Select(e => e.ToString())))}]");
		if (HeatingElementsAdded?.Length > 0)
			parts.Add($"HeatingElementsAdded = [{(string.Join(", ", HeatingElementsAdded.Select(e => e.ToString())))}]");

		if (HeatingElementsRemoved?.Length > 0)
			parts.Add($"HeatingElementsRemoved = [{(string.Join(", ", HeatingElementsRemoved.Select(e => e.ToString())))}]");

		if (HeatingElementsUpdated?.Length > 0)
			parts.Add($"HeatingElementsUpdated = [{(string.Join(", ", HeatingElementsUpdated.Select(e => e.ToString())))}]");
		if (StreamingOMEURLHasChanged) 
			parts.Add($"StreamingOMEURL = Previous: {StreamingOMEURLPrevious}, New: {StreamingOMEURLNew}");
		if (ThumbnailOMEURLHasChanged) 
			parts.Add($"ThumbnailOMEURL = Previous: {ThumbnailOMEURLPrevious}, New: {ThumbnailOMEURLNew}");
		if (NotificationsAdded?.Length > 0)
			parts.Add($"NotificationsAdded = [{(string.Join(", ", NotificationsAdded.Select(e => e.ToString())))}]");

		if (NotificationsRemoved?.Length > 0)
			parts.Add($"NotificationsRemoved = [{(string.Join(", ", NotificationsRemoved.Select(e => e.ToString())))}]");

		if (NotificationsUpdated?.Length > 0)
			parts.Add($"NotificationsUpdated = [{(string.Join(", ", NotificationsUpdated.Select(e => e.ToString())))}]");
		return $"MachineStateChanges {(string.Join(", ", parts))}";
	}
}
