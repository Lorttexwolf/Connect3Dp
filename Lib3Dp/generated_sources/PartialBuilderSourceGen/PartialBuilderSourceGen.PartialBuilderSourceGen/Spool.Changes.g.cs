#nullable enable
namespace Lib3Dp.State;

public readonly record struct SpoolChanges(
	bool NumberHasChanged,
	int? NumberPrevious,
	int? NumberNew,
	MaterialChanges? MaterialChanges,
	bool GramsMaximumHasChanged,
	int? GramsMaximumPrevious,
	int? GramsMaximumNew,
	bool GramsRemainingHasChanged,
	int? GramsRemainingPrevious,
	int? GramsRemainingNew
)
{
    public bool HasChanged => NumberHasChanged || MaterialChanges?.HasChanged == true || GramsMaximumHasChanged || GramsRemainingHasChanged;

	public override string ToString()
	{
		if (!HasChanged) return $"{nameof(SpoolChanges)}";
		var parts = new List<string>();
		if (NumberHasChanged) 
			parts.Add($"Number = Previous: {NumberPrevious}, New: {NumberNew}");
		if (MaterialChanges?.HasChanged == true)
			parts.Add($"Material = {MaterialChanges}");
		if (GramsMaximumHasChanged) 
			parts.Add($"GramsMaximum = Previous: {GramsMaximumPrevious}, New: {GramsMaximumNew}");
		if (GramsRemainingHasChanged) 
			parts.Add($"GramsRemaining = Previous: {GramsRemainingPrevious}, New: {GramsRemainingNew}");
		return $"SpoolChanges {(string.Join(", ", parts))}";
	}
}
