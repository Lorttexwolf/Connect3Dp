#nullable enable
namespace Lib3Dp.State;

public readonly record struct PrintJobChanges(
	bool NameHasChanged,
	string? NamePrevious,
	string? NameNew,
	bool CustomIDHasChanged,
	string? CustomIDPrevious,
	string? CustomIDNew,
	bool PercentageCompleteHasChanged,
	int? PercentageCompletePrevious,
	int? PercentageCompleteNew,
	bool RemainingTimeHasChanged,
	System.TimeSpan? RemainingTimePrevious,
	System.TimeSpan? RemainingTimeNew,
	bool TotalTimeHasChanged,
	System.TimeSpan? TotalTimePrevious,
	System.TimeSpan? TotalTimeNew,
	bool IssueHasChanged,
	Lib3Dp.State.MachineMessage? IssuePrevious,
	Lib3Dp.State.MachineMessage? IssueNew,
	bool ThumbnailHasChanged,
	Lib3Dp.Files.MachineFileHandle? ThumbnailPrevious,
	Lib3Dp.Files.MachineFileHandle? ThumbnailNew,
	bool FileHasChanged,
	Lib3Dp.Files.MachineFileHandle? FilePrevious,
	Lib3Dp.Files.MachineFileHandle? FileNew,
	bool SubStageHasChanged,
	string? SubStagePrevious,
	string? SubStageNew,
	bool TotalMaterialUsageHasChanged,
	int? TotalMaterialUsagePrevious,
	int? TotalMaterialUsageNew,
	bool LocalPathHasChanged,
	string? LocalPathPrevious,
	string? LocalPathNew,
	KeyValuePair<Lib3Dp.State.SpoolLocation, int>[] SpoolMaterialUsagesAdded,
	Lib3Dp.State.SpoolLocation[] SpoolMaterialUsagesRemoved,
	KeyValuePair<Lib3Dp.State.SpoolLocation, int>[] SpoolMaterialUsagesUpdated
)
{
    public bool HasChanged => NameHasChanged || CustomIDHasChanged || PercentageCompleteHasChanged || RemainingTimeHasChanged || TotalTimeHasChanged || IssueHasChanged || ThumbnailHasChanged || FileHasChanged || SubStageHasChanged || TotalMaterialUsageHasChanged || LocalPathHasChanged || SpoolMaterialUsagesAdded?.Length > 0 || SpoolMaterialUsagesRemoved?.Length > 0 || SpoolMaterialUsagesUpdated?.Length > 0;

	public override string ToString()
	{
		if (!HasChanged) return $"{nameof(PrintJobChanges)}";
		var parts = new List<string>();
		if (NameHasChanged) 
			parts.Add($"Name = Previous: {NamePrevious}, New: {NameNew}");
		if (CustomIDHasChanged) 
			parts.Add($"CustomID = Previous: {CustomIDPrevious}, New: {CustomIDNew}");
		if (PercentageCompleteHasChanged) 
			parts.Add($"PercentageComplete = Previous: {PercentageCompletePrevious}, New: {PercentageCompleteNew}");
		if (RemainingTimeHasChanged) 
			parts.Add($"RemainingTime = Previous: {RemainingTimePrevious}, New: {RemainingTimeNew}");
		if (TotalTimeHasChanged) 
			parts.Add($"TotalTime = Previous: {TotalTimePrevious}, New: {TotalTimeNew}");
		if (IssueHasChanged) 
			parts.Add($"Issue = Previous: {IssuePrevious}, New: {IssueNew}");
		if (ThumbnailHasChanged) 
			parts.Add($"Thumbnail = Previous: {ThumbnailPrevious}, New: {ThumbnailNew}");
		if (FileHasChanged) 
			parts.Add($"File = Previous: {FilePrevious}, New: {FileNew}");
		if (SubStageHasChanged) 
			parts.Add($"SubStage = Previous: {SubStagePrevious}, New: {SubStageNew}");
		if (TotalMaterialUsageHasChanged) 
			parts.Add($"TotalMaterialUsage = Previous: {TotalMaterialUsagePrevious}, New: {TotalMaterialUsageNew}");
		if (LocalPathHasChanged) 
			parts.Add($"LocalPath = Previous: {LocalPathPrevious}, New: {LocalPathNew}");
		if (SpoolMaterialUsagesAdded?.Length > 0)
			parts.Add($"SpoolMaterialUsagesAdded = [{(string.Join(", ", SpoolMaterialUsagesAdded.Select(e => e.ToString())))}]");

		if (SpoolMaterialUsagesRemoved?.Length > 0)
			parts.Add($"SpoolMaterialUsagesRemoved = [{(string.Join(", ", SpoolMaterialUsagesRemoved.Select(e => e.ToString())))}]");

		if (SpoolMaterialUsagesUpdated?.Length > 0)
			parts.Add($"SpoolMaterialUsagesUpdated = [{(string.Join(", ", SpoolMaterialUsagesUpdated.Select(e => e.ToString())))}]");
		return $"PrintJobChanges {(string.Join(", ", parts))}";
	}
}
