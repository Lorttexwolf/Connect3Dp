#nullable enable
namespace Lib3Dp.State;

public sealed class PrintJobUpdate
{
    public string Name { get; private set; }
    public bool NameIsSet { get; private set; }
    public string CustomID { get; private set; }
    public bool CustomIDIsSet { get; private set; }
    public int PercentageComplete { get; private set; }
    public bool PercentageCompleteIsSet { get; private set; }
    public System.TimeSpan RemainingTime { get; private set; }
    public bool RemainingTimeIsSet { get; private set; }
    public System.TimeSpan TotalTime { get; private set; }
    public bool TotalTimeIsSet { get; private set; }
    public Lib3Dp.State.MachineMessage? Issue { get; private set; }
    public bool IssueIsSet { get; private set; }
    public Lib3Dp.Files.MachineFileHandle? Thumbnail { get; private set; }
    public bool ThumbnailIsSet { get; private set; }
    public Lib3Dp.Files.MachineFileHandle? File { get; private set; }
    public bool FileIsSet { get; private set; }
    public string SubStage { get; private set; }
    public bool SubStageIsSet { get; private set; }
    public int? TotalMaterialUsage { get; private set; }
    public bool TotalMaterialUsageIsSet { get; private set; }
    public string LocalPath { get; private set; }
    public bool LocalPathIsSet { get; private set; }
    public Dictionary<Lib3Dp.State.SpoolLocation,int>? SpoolMaterialUsagesToSet { get; private set; }
    public HashSet<Lib3Dp.State.SpoolLocation>? SpoolMaterialUsagesToRemove { get; private set; }

public PrintJobUpdate SetName(string value)
{
	NameIsSet = true;
	Name = value;
	return this;
}

public PrintJobUpdate RemoveName()
{
	NameIsSet = true;
	Name = default;
	return this;
}
public PrintJobUpdate UnsetName()
{
	NameIsSet = false;
	Name = default;
	return this;
}
public PrintJobUpdate SetCustomID(string value)
{
	CustomIDIsSet = true;
	CustomID = value;
	return this;
}

public PrintJobUpdate RemoveCustomID()
{
	CustomIDIsSet = true;
	CustomID = default;
	return this;
}
public PrintJobUpdate UnsetCustomID()
{
	CustomIDIsSet = false;
	CustomID = default;
	return this;
}
public PrintJobUpdate SetPercentageComplete(int value)
{
	PercentageCompleteIsSet = true;
	PercentageComplete = value;
	return this;
}

public PrintJobUpdate RemovePercentageComplete()
{
	PercentageCompleteIsSet = true;
	PercentageComplete = default;
	return this;
}
public PrintJobUpdate UnsetPercentageComplete()
{
	PercentageCompleteIsSet = false;
	PercentageComplete = default;
	return this;
}
public PrintJobUpdate SetRemainingTime(System.TimeSpan value)
{
	RemainingTimeIsSet = true;
	RemainingTime = value;
	return this;
}

public PrintJobUpdate RemoveRemainingTime()
{
	RemainingTimeIsSet = true;
	RemainingTime = default;
	return this;
}
public PrintJobUpdate UnsetRemainingTime()
{
	RemainingTimeIsSet = false;
	RemainingTime = default;
	return this;
}
public PrintJobUpdate SetTotalTime(System.TimeSpan value)
{
	TotalTimeIsSet = true;
	TotalTime = value;
	return this;
}

public PrintJobUpdate RemoveTotalTime()
{
	TotalTimeIsSet = true;
	TotalTime = default;
	return this;
}
public PrintJobUpdate UnsetTotalTime()
{
	TotalTimeIsSet = false;
	TotalTime = default;
	return this;
}
public PrintJobUpdate SetIssue(Lib3Dp.State.MachineMessage? value)
{
	IssueIsSet = true;
	Issue = value;
	return this;
}

public PrintJobUpdate RemoveIssue()
{
	IssueIsSet = true;
	Issue = default;
	return this;
}
public PrintJobUpdate UnsetIssue()
{
	IssueIsSet = false;
	Issue = default;
	return this;
}
public PrintJobUpdate SetThumbnail(Lib3Dp.Files.MachineFileHandle? value)
{
	ThumbnailIsSet = true;
	Thumbnail = value;
	return this;
}

public PrintJobUpdate RemoveThumbnail()
{
	ThumbnailIsSet = true;
	Thumbnail = default;
	return this;
}
public PrintJobUpdate UnsetThumbnail()
{
	ThumbnailIsSet = false;
	Thumbnail = default;
	return this;
}
public PrintJobUpdate SetFile(Lib3Dp.Files.MachineFileHandle? value)
{
	FileIsSet = true;
	File = value;
	return this;
}

public PrintJobUpdate RemoveFile()
{
	FileIsSet = true;
	File = default;
	return this;
}
public PrintJobUpdate UnsetFile()
{
	FileIsSet = false;
	File = default;
	return this;
}
public PrintJobUpdate SetSubStage(string value)
{
	SubStageIsSet = true;
	SubStage = value;
	return this;
}

public PrintJobUpdate RemoveSubStage()
{
	SubStageIsSet = true;
	SubStage = default;
	return this;
}
public PrintJobUpdate UnsetSubStage()
{
	SubStageIsSet = false;
	SubStage = default;
	return this;
}
public PrintJobUpdate SetTotalMaterialUsage(int? value)
{
	TotalMaterialUsageIsSet = true;
	TotalMaterialUsage = value;
	return this;
}

public PrintJobUpdate RemoveTotalMaterialUsage()
{
	TotalMaterialUsageIsSet = true;
	TotalMaterialUsage = default;
	return this;
}
public PrintJobUpdate UnsetTotalMaterialUsage()
{
	TotalMaterialUsageIsSet = false;
	TotalMaterialUsage = default;
	return this;
}
public PrintJobUpdate SetLocalPath(string value)
{
	LocalPathIsSet = true;
	LocalPath = value;
	return this;
}

public PrintJobUpdate RemoveLocalPath()
{
	LocalPathIsSet = true;
	LocalPath = default;
	return this;
}
public PrintJobUpdate UnsetLocalPath()
{
	LocalPathIsSet = false;
	LocalPath = default;
	return this;
}
	public PrintJobUpdate SetSpoolMaterialUsages(Lib3Dp.State.SpoolLocation key, int val)
	{
		SpoolMaterialUsagesToSet ??= new Dictionary<Lib3Dp.State.SpoolLocation, int>();		
		SpoolMaterialUsagesToSet[key] = val;
		return this;
	}

	public PrintJobUpdate RemoveSpoolMaterialUsages(Lib3Dp.State.SpoolLocation key)
	{
		SpoolMaterialUsagesToRemove ??= new HashSet<Lib3Dp.State.SpoolLocation>();
		SpoolMaterialUsagesToRemove.Add(key);
		return this;
	}
public PrintJobUpdate UnsetSpoolMaterialUsages()
{
	SpoolMaterialUsagesToSet = null;
	SpoolMaterialUsagesToRemove = null;
	return this;
}
    public bool TryCreate(out PrintJob? outResult)
    {
		outResult = null;
        if (!NameIsSet) return false;
        if (!PercentageCompleteIsSet) return false;
        if (!RemainingTimeIsSet) return false;
        if (!TotalTimeIsSet) return false;
        var result = new PrintJob() { Name = this.Name, PercentageComplete = this.PercentageComplete, RemainingTime = this.RemainingTime, TotalTime = this.TotalTime };
        AppendUpdate(result, out _);
		outResult = result;

        return true;
    }
    public PrintJobChanges Changes(PrintJob printJob)
    {
		if (printJob == null) return default;
		var __Name_hasChanged = false;
		string? __Name_prev = null;
		string? __Name_new = null;
		if (this.NameIsSet)
		{
			if (!EqualityComparer<string>.Default.Equals(printJob.Name, this.Name))
			{
				__Name_hasChanged = true;
				__Name_prev = printJob.Name;
				__Name_new = this.Name;
			}
		}

		var __CustomID_hasChanged = false;
		string? __CustomID_prev = null;
		string? __CustomID_new = null;
		if (this.CustomIDIsSet)
		{
			if (!EqualityComparer<string>.Default.Equals(printJob.CustomID, this.CustomID))
			{
				__CustomID_hasChanged = true;
				__CustomID_prev = printJob.CustomID;
				__CustomID_new = this.CustomID;
			}
		}

		var __PercentageComplete_hasChanged = false;
		int? __PercentageComplete_prev = null;
		int? __PercentageComplete_new = null;
		if (this.PercentageCompleteIsSet)
		{
			if (!EqualityComparer<int>.Default.Equals(printJob.PercentageComplete, this.PercentageComplete))
			{
				__PercentageComplete_hasChanged = true;
				__PercentageComplete_prev = printJob.PercentageComplete;
				__PercentageComplete_new = this.PercentageComplete;
			}
		}

		var __RemainingTime_hasChanged = false;
		System.TimeSpan? __RemainingTime_prev = null;
		System.TimeSpan? __RemainingTime_new = null;
		if (this.RemainingTimeIsSet)
		{
			if (!EqualityComparer<System.TimeSpan>.Default.Equals(printJob.RemainingTime, this.RemainingTime))
			{
				__RemainingTime_hasChanged = true;
				__RemainingTime_prev = printJob.RemainingTime;
				__RemainingTime_new = this.RemainingTime;
			}
		}

		var __TotalTime_hasChanged = false;
		System.TimeSpan? __TotalTime_prev = null;
		System.TimeSpan? __TotalTime_new = null;
		if (this.TotalTimeIsSet)
		{
			if (!EqualityComparer<System.TimeSpan>.Default.Equals(printJob.TotalTime, this.TotalTime))
			{
				__TotalTime_hasChanged = true;
				__TotalTime_prev = printJob.TotalTime;
				__TotalTime_new = this.TotalTime;
			}
		}

		var __Issue_hasChanged = false;
		Lib3Dp.State.MachineMessage? __Issue_prev = null;
		Lib3Dp.State.MachineMessage? __Issue_new = null;
		if (this.IssueIsSet)
		{
			if (!EqualityComparer<Lib3Dp.State.MachineMessage?>.Default.Equals(printJob.Issue, this.Issue))
			{
				__Issue_hasChanged = true;
				__Issue_prev = printJob.Issue;
				__Issue_new = this.Issue;
			}
		}

		var __Thumbnail_hasChanged = false;
		Lib3Dp.Files.MachineFileHandle? __Thumbnail_prev = null;
		Lib3Dp.Files.MachineFileHandle? __Thumbnail_new = null;
		if (this.ThumbnailIsSet)
		{
			if (!EqualityComparer<Lib3Dp.Files.MachineFileHandle?>.Default.Equals(printJob.Thumbnail, this.Thumbnail))
			{
				__Thumbnail_hasChanged = true;
				__Thumbnail_prev = printJob.Thumbnail;
				__Thumbnail_new = this.Thumbnail;
			}
		}

		var __File_hasChanged = false;
		Lib3Dp.Files.MachineFileHandle? __File_prev = null;
		Lib3Dp.Files.MachineFileHandle? __File_new = null;
		if (this.FileIsSet)
		{
			if (!EqualityComparer<Lib3Dp.Files.MachineFileHandle?>.Default.Equals(printJob.File, this.File))
			{
				__File_hasChanged = true;
				__File_prev = printJob.File;
				__File_new = this.File;
			}
		}

		var __SubStage_hasChanged = false;
		string? __SubStage_prev = null;
		string? __SubStage_new = null;
		if (this.SubStageIsSet)
		{
			if (!EqualityComparer<string>.Default.Equals(printJob.SubStage, this.SubStage))
			{
				__SubStage_hasChanged = true;
				__SubStage_prev = printJob.SubStage;
				__SubStage_new = this.SubStage;
			}
		}

		var __TotalMaterialUsage_hasChanged = false;
		int? __TotalMaterialUsage_prev = null;
		int? __TotalMaterialUsage_new = null;
		if (this.TotalMaterialUsageIsSet)
		{
			if (!EqualityComparer<int?>.Default.Equals(printJob.TotalMaterialUsage, this.TotalMaterialUsage))
			{
				__TotalMaterialUsage_hasChanged = true;
				__TotalMaterialUsage_prev = printJob.TotalMaterialUsage;
				__TotalMaterialUsage_new = this.TotalMaterialUsage;
			}
		}

		var __LocalPath_hasChanged = false;
		string? __LocalPath_prev = null;
		string? __LocalPath_new = null;
		if (this.LocalPathIsSet)
		{
			if (!EqualityComparer<string>.Default.Equals(printJob.LocalPath, this.LocalPath))
			{
				__LocalPath_hasChanged = true;
				__LocalPath_prev = printJob.LocalPath;
				__LocalPath_new = this.LocalPath;
			}
		}

		var aSpoolMaterialUsages_added = new List<KeyValuePair<Lib3Dp.State.SpoolLocation, int>>();
		var uSpoolMaterialUsages_updated = new List<KeyValuePair<Lib3Dp.State.SpoolLocation, int>>();
		var rSpoolMaterialUsages_removed = new List<Lib3Dp.State.SpoolLocation>();
		if (this.SpoolMaterialUsagesToSet != null)
		{
			foreach (var kv in this.SpoolMaterialUsagesToSet)
			{
				if (printJob.SpoolMaterialUsages != null && printJob.SpoolMaterialUsages.TryGetValue(kv.Key, out var existing))
				{
					if (!EqualityComparer<int>.Default.Equals(existing, kv.Value))
						uSpoolMaterialUsages_updated.Add(kv);
				}
				else
					aSpoolMaterialUsages_added.Add(kv);
			}
		}
		if (this.SpoolMaterialUsagesToRemove != null && printJob.SpoolMaterialUsages != null)
		{
			foreach (var k in this.SpoolMaterialUsagesToRemove)
			{
				if (printJob.SpoolMaterialUsages.ContainsKey(k)) rSpoolMaterialUsages_removed.Add(k);
			}
		}

		return new PrintJobChanges(__Name_hasChanged, __Name_prev, __Name_new, __CustomID_hasChanged, __CustomID_prev, __CustomID_new, __PercentageComplete_hasChanged, __PercentageComplete_prev, __PercentageComplete_new, __RemainingTime_hasChanged, __RemainingTime_prev, __RemainingTime_new, __TotalTime_hasChanged, __TotalTime_prev, __TotalTime_new, __Issue_hasChanged, __Issue_prev, __Issue_new, __Thumbnail_hasChanged, __Thumbnail_prev, __Thumbnail_new, __File_hasChanged, __File_prev, __File_new, __SubStage_hasChanged, __SubStage_prev, __SubStage_new, __TotalMaterialUsage_hasChanged, __TotalMaterialUsage_prev, __TotalMaterialUsage_new, __LocalPath_hasChanged, __LocalPath_prev, __LocalPath_new, aSpoolMaterialUsages_added.ToArray(), rSpoolMaterialUsages_removed.ToArray(), uSpoolMaterialUsages_updated.ToArray());
    }

    public void AppendUpdate(PrintJob printJob, out PrintJobChanges changes)
    {
		changes = Changes(printJob);

		if (this.NameIsSet)
		{
			printJob.Name = this.Name;
		}

		if (this.CustomIDIsSet)
		{
			printJob.CustomID = this.CustomID;
		}

		if (this.PercentageCompleteIsSet)
		{
			printJob.PercentageComplete = this.PercentageComplete;
		}

		if (this.RemainingTimeIsSet)
		{
			printJob.RemainingTime = this.RemainingTime;
		}

		if (this.TotalTimeIsSet)
		{
			printJob.TotalTime = this.TotalTime;
		}

		if (this.IssueIsSet)
		{
			printJob.Issue = this.Issue;
		}

		if (this.ThumbnailIsSet)
		{
			printJob.Thumbnail = this.Thumbnail;
		}

		if (this.FileIsSet)
		{
			printJob.File = this.File;
		}

		if (this.SubStageIsSet)
		{
			printJob.SubStage = this.SubStage;
		}

		if (this.TotalMaterialUsageIsSet)
		{
			printJob.TotalMaterialUsage = this.TotalMaterialUsage;
		}

		if (this.LocalPathIsSet)
		{
			printJob.LocalPath = this.LocalPath;
		}

		if (this.SpoolMaterialUsagesToSet != null)
		{
			foreach (var kv in this.SpoolMaterialUsagesToSet)
				printJob.SpoolMaterialUsages[kv.Key] = kv.Value;
		}
		if (this.SpoolMaterialUsagesToRemove != null && printJob.SpoolMaterialUsages != null)
		{
			foreach (var k in this.SpoolMaterialUsagesToRemove) printJob.SpoolMaterialUsages.Remove(k);
		}

    }

}
