#nullable enable
namespace Lib3Dp.State;

public readonly record struct MachineExtruderChanges(
	bool NumberHasChanged,
	int? NumberPrevious,
	int? NumberNew,
	bool HeatingConstraintHasChanged,
	Lib3Dp.State.HeatingConstraints? HeatingConstraintPrevious,
	Lib3Dp.State.HeatingConstraints? HeatingConstraintNew,
	bool TempCHasChanged,
	double? TempCPrevious,
	double? TempCNew,
	bool TargetTempCHasChanged,
	double? TargetTempCPrevious,
	double? TargetTempCNew,
	bool NozzleNumberHasChanged,
	int? NozzleNumberPrevious,
	int? NozzleNumberNew,
	bool LoadedSpoolHasChanged,
	Lib3Dp.State.SpoolLocation? LoadedSpoolPrevious,
	Lib3Dp.State.SpoolLocation? LoadedSpoolNew
)
{
    public bool HasChanged => NumberHasChanged || HeatingConstraintHasChanged || TempCHasChanged || TargetTempCHasChanged || NozzleNumberHasChanged || LoadedSpoolHasChanged;

	public override string ToString()
	{
		if (!HasChanged) return $"{nameof(MachineExtruderChanges)}";
		var parts = new List<string>();
		if (NumberHasChanged) 
			parts.Add($"Number = Previous: {NumberPrevious}, New: {NumberNew}");
		if (HeatingConstraintHasChanged) 
			parts.Add($"HeatingConstraint = Previous: {HeatingConstraintPrevious}, New: {HeatingConstraintNew}");
		if (TempCHasChanged) 
			parts.Add($"TempC = Previous: {TempCPrevious}, New: {TempCNew}");
		if (TargetTempCHasChanged) 
			parts.Add($"TargetTempC = Previous: {TargetTempCPrevious}, New: {TargetTempCNew}");
		if (NozzleNumberHasChanged) 
			parts.Add($"NozzleNumber = Previous: {NozzleNumberPrevious}, New: {NozzleNumberNew}");
		if (LoadedSpoolHasChanged) 
			parts.Add($"LoadedSpool = Previous: {LoadedSpoolPrevious}, New: {LoadedSpoolNew}");
		return $"MachineExtruderChanges {(string.Join(", ", parts))}";
	}
}
