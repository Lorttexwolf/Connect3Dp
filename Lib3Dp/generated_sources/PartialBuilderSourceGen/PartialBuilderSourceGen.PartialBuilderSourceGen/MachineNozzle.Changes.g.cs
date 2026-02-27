#nullable enable
namespace Lib3Dp.State;

public readonly record struct MachineNozzleChanges(
	bool NumberHasChanged,
	int? NumberPrevious,
	int? NumberNew,
	bool DiameterHasChanged,
	double? DiameterPrevious,
	double? DiameterNew
)
{
    public bool HasChanged => NumberHasChanged || DiameterHasChanged;

	public override string ToString()
	{
		if (!HasChanged) return $"{nameof(MachineNozzleChanges)}";
		var parts = new List<string>();
		if (NumberHasChanged) 
			parts.Add($"Number = Previous: {NumberPrevious}, New: {NumberNew}");
		if (DiameterHasChanged) 
			parts.Add($"Diameter = Previous: {DiameterPrevious}, New: {DiameterNew}");
		return $"MachineNozzleChanges {(string.Join(", ", parts))}";
	}
}
