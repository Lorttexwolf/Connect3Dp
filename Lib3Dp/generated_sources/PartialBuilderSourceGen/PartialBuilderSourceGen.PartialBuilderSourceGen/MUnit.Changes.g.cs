#nullable enable
namespace Lib3Dp.State;

public readonly record struct MUnitChanges(
	bool IDHasChanged,
	string? IDPrevious,
	string? IDNew,
	bool CapacityHasChanged,
	int? CapacityPrevious,
	int? CapacityNew,
	bool ModelHasChanged,
	string? ModelPrevious,
	string? ModelNew,
	bool CapabilitiesHasChanged,
	Lib3Dp.State.MUCapabilities? CapabilitiesPrevious,
	Lib3Dp.State.MUCapabilities? CapabilitiesNew,
	bool HeatingConstraintsHasChanged,
	Lib3Dp.State.HeatingConstraints? HeatingConstraintsPrevious,
	Lib3Dp.State.HeatingConstraints? HeatingConstraintsNew,
	KeyValuePair<int, Lib3Dp.State.Spool>[] TraysAdded,
	int[] TraysRemoved,
	KeyValuePair<int, SpoolChanges>[] TraysUpdated,
	bool HumidityPercentHasChanged,
	double? HumidityPercentPrevious,
	double? HumidityPercentNew,
	bool TemperatureCHasChanged,
	double? TemperatureCPrevious,
	double? TemperatureCNew,
	bool HeatingJobHasChanged,
	Lib3Dp.State.HeatingJob? HeatingJobPrevious,
	Lib3Dp.State.HeatingJob? HeatingJobNew,
	Lib3Dp.State.HeatingSchedule[] HeatingScheduleAdded,
	Lib3Dp.State.HeatingSchedule[] HeatingScheduleRemoved
)
{
    public bool HasChanged => IDHasChanged || CapacityHasChanged || ModelHasChanged || CapabilitiesHasChanged || HeatingConstraintsHasChanged || TraysAdded?.Length > 0 || TraysRemoved?.Length > 0 || TraysUpdated?.Length > 0 || HumidityPercentHasChanged || TemperatureCHasChanged || HeatingJobHasChanged || HeatingScheduleAdded?.Length > 0 || HeatingScheduleRemoved?.Length > 0;

	public override string ToString()
	{
		if (!HasChanged) return $"{nameof(MUnitChanges)}";
		var parts = new List<string>();
		if (IDHasChanged) 
			parts.Add($"ID = Previous: {IDPrevious}, New: {IDNew}");
		if (CapacityHasChanged) 
			parts.Add($"Capacity = Previous: {CapacityPrevious}, New: {CapacityNew}");
		if (ModelHasChanged) 
			parts.Add($"Model = Previous: {ModelPrevious}, New: {ModelNew}");
		if (CapabilitiesHasChanged) 
			parts.Add($"Capabilities = Previous: {CapabilitiesPrevious}, New: {CapabilitiesNew}");
		if (HeatingConstraintsHasChanged) 
			parts.Add($"HeatingConstraints = Previous: {HeatingConstraintsPrevious}, New: {HeatingConstraintsNew}");
		if (TraysAdded?.Length > 0)
			parts.Add($"TraysAdded = [{(string.Join(", ", TraysAdded.Select(e => e.ToString())))}]");

		if (TraysRemoved?.Length > 0)
			parts.Add($"TraysRemoved = [{(string.Join(", ", TraysRemoved.Select(e => e.ToString())))}]");

		if (TraysUpdated?.Length > 0)
			parts.Add($"TraysUpdated = [{(string.Join(", ", TraysUpdated.Select(e => e.ToString())))}]");
		if (HumidityPercentHasChanged) 
			parts.Add($"HumidityPercent = Previous: {HumidityPercentPrevious}, New: {HumidityPercentNew}");
		if (TemperatureCHasChanged) 
			parts.Add($"TemperatureC = Previous: {TemperatureCPrevious}, New: {TemperatureCNew}");
		if (HeatingJobHasChanged) 
			parts.Add($"HeatingJob = Previous: {HeatingJobPrevious}, New: {HeatingJobNew}");
		if (HeatingScheduleAdded?.Length > 0)
			parts.Add($"HeatingScheduleAdded = [{(string.Join(", ", HeatingScheduleAdded.Select(e => e.ToString())))}]");

		if (HeatingScheduleRemoved?.Length > 0)
			parts.Add($"HeatingScheduleRemoved = [{(string.Join(", ", HeatingScheduleRemoved.Select(e => e.ToString())))}]");
		return $"MUnitChanges {(string.Join(", ", parts))}";
	}
}
