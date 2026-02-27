#nullable enable
namespace Lib3Dp.State;

public readonly record struct MaterialChanges(
	bool NameHasChanged,
	string? NamePrevious,
	string? NameNew,
	bool ColorHasChanged,
	Lib3Dp.State.MaterialColor? ColorPrevious,
	Lib3Dp.State.MaterialColor? ColorNew,
	bool FProfileIDXHasChanged,
	string? FProfileIDXPrevious,
	string? FProfileIDXNew
)
{
    public bool HasChanged => NameHasChanged || ColorHasChanged || FProfileIDXHasChanged;

	public override string ToString()
	{
		if (!HasChanged) return $"{nameof(MaterialChanges)}";
		var parts = new List<string>();
		if (NameHasChanged) 
			parts.Add($"Name = Previous: {NamePrevious}, New: {NameNew}");
		if (ColorHasChanged) 
			parts.Add($"Color = Previous: {ColorPrevious}, New: {ColorNew}");
		if (FProfileIDXHasChanged) 
			parts.Add($"FProfileIDX = Previous: {FProfileIDXPrevious}, New: {FProfileIDXNew}");
		return $"MaterialChanges {(string.Join(", ", parts))}";
	}
}
