#nullable enable
namespace Lib3Dp.State;

public sealed class MachineStateUpdate
{
    public string ID { get; private set; }
    public bool IDIsSet { get; private set; }
    public string Brand { get; private set; }
    public bool BrandIsSet { get; private set; }
    public string Model { get; private set; }
    public bool ModelIsSet { get; private set; }
    public string Nickname { get; private set; }
    public bool NicknameIsSet { get; private set; }
    public Lib3Dp.State.MachineCapabilities Capabilities { get; private set; }
    public bool CapabilitiesIsSet { get; private set; }
    public Lib3Dp.State.MachineStatus Status { get; private set; }
    public bool StatusIsSet { get; private set; }
    public PrintJobUpdate? CurrentJob { get; private set; }
    public bool CurrentJobIsSet { get; private set; }
    public HashSet<Lib3Dp.State.HistoricPrintJob>? JobHistoryToSet { get; private set; }
    public HashSet<Lib3Dp.State.HistoricPrintJob>? JobHistoryToRemove { get; private set; }
    public HashSet<Lib3Dp.State.LocalPrintJob>? LocalJobsToSet { get; private set; }
    public HashSet<Lib3Dp.State.LocalPrintJob>? LocalJobsToRemove { get; private set; }
    public HashSet<Lib3Dp.State.ScheduledPrint>? ScheduledPrintsToSet { get; private set; }
    public HashSet<Lib3Dp.State.ScheduledPrint>? ScheduledPrintsToRemove { get; private set; }
    public Dictionary<int,MachineExtruderUpdate>? ExtrudersToSet { get; private set; }
    public HashSet<int>? ExtrudersToRemove { get; private set; }
    public Dictionary<int,MachineNozzleUpdate>? NozzlesToSet { get; private set; }
    public HashSet<int>? NozzlesToRemove { get; private set; }
    public Dictionary<string,MUnitUpdate>? MaterialUnitsToSet { get; private set; }
    public HashSet<string>? MaterialUnitsToRemove { get; private set; }
    public Lib3Dp.State.MachineAirDuctMode AirDuctMode { get; private set; }
    public bool AirDuctModeIsSet { get; private set; }
    public Dictionary<string,int>? FansToSet { get; private set; }
    public HashSet<string>? FansToRemove { get; private set; }
    public Dictionary<string,bool>? LightsToSet { get; private set; }
    public HashSet<string>? LightsToRemove { get; private set; }
    public Dictionary<string,Lib3Dp.State.HeatingElement>? HeatingElementsToSet { get; private set; }
    public HashSet<string>? HeatingElementsToRemove { get; private set; }
    public string StreamingOMEURL { get; private set; }
    public bool StreamingOMEURLIsSet { get; private set; }
    public string ThumbnailOMEURL { get; private set; }
    public bool ThumbnailOMEURLIsSet { get; private set; }
    public Dictionary<Lib3Dp.State.MachineMessage,NotificationUpdate>? NotificationsToSet { get; private set; }
    public HashSet<Lib3Dp.State.MachineMessage>? NotificationsToRemove { get; private set; }

public MachineStateUpdate SetID(string value)
{
	IDIsSet = true;
	ID = value;
	return this;
}

public MachineStateUpdate RemoveID()
{
	IDIsSet = true;
	ID = default;
	return this;
}
public MachineStateUpdate UnsetID()
{
	IDIsSet = false;
	ID = default;
	return this;
}
public MachineStateUpdate SetBrand(string value)
{
	BrandIsSet = true;
	Brand = value;
	return this;
}

public MachineStateUpdate RemoveBrand()
{
	BrandIsSet = true;
	Brand = default;
	return this;
}
public MachineStateUpdate UnsetBrand()
{
	BrandIsSet = false;
	Brand = default;
	return this;
}
public MachineStateUpdate SetModel(string value)
{
	ModelIsSet = true;
	Model = value;
	return this;
}

public MachineStateUpdate RemoveModel()
{
	ModelIsSet = true;
	Model = default;
	return this;
}
public MachineStateUpdate UnsetModel()
{
	ModelIsSet = false;
	Model = default;
	return this;
}
public MachineStateUpdate SetNickname(string value)
{
	NicknameIsSet = true;
	Nickname = value;
	return this;
}

public MachineStateUpdate RemoveNickname()
{
	NicknameIsSet = true;
	Nickname = default;
	return this;
}
public MachineStateUpdate UnsetNickname()
{
	NicknameIsSet = false;
	Nickname = default;
	return this;
}
public MachineStateUpdate SetCapabilities(Lib3Dp.State.MachineCapabilities value)
{
	CapabilitiesIsSet = true;
	Capabilities = value;
	return this;
}

public MachineStateUpdate RemoveCapabilities()
{
	CapabilitiesIsSet = true;
	Capabilities = default;
	return this;
}
public MachineStateUpdate UnsetCapabilities()
{
	CapabilitiesIsSet = false;
	Capabilities = default;
	return this;
}
public MachineStateUpdate SetStatus(Lib3Dp.State.MachineStatus value)
{
	StatusIsSet = true;
	Status = value;
	return this;
}

public MachineStateUpdate RemoveStatus()
{
	StatusIsSet = true;
	Status = default;
	return this;
}
public MachineStateUpdate UnsetStatus()
{
	StatusIsSet = false;
	Status = default;
	return this;
}
public MachineStateUpdate UpdateCurrentJob(Action<PrintJobUpdate> configure)
{
	CurrentJobIsSet = true;
	CurrentJob ??= new PrintJobUpdate();
	configure(CurrentJob);
	return this;
}

public MachineStateUpdate RemoveCurrentJob()
{
	CurrentJobIsSet = true;
	CurrentJob = null;
	return this;
}
public MachineStateUpdate UnsetCurrentJob()
{
	CurrentJobIsSet = false;
	CurrentJob = null;
	return this;
}
public MachineStateUpdate SetJobHistory(Lib3Dp.State.HistoricPrintJob jobHistory)
{
	JobHistoryToSet ??= new HashSet<Lib3Dp.State.HistoricPrintJob>();
	JobHistoryToSet.Add(jobHistory);
	return this;
}

public MachineStateUpdate RemoveJobHistory(Lib3Dp.State.HistoricPrintJob jobHistory)
{
	JobHistoryToRemove ??= new HashSet<Lib3Dp.State.HistoricPrintJob>();
	JobHistoryToRemove.Add(jobHistory);
	return this;
}
public MachineStateUpdate UnsetJobHistory()
{
	JobHistoryToSet = null;
	JobHistoryToRemove = null;
	return this;
}
public MachineStateUpdate SetLocalJobs(Lib3Dp.State.LocalPrintJob localJobs)
{
	LocalJobsToSet ??= new HashSet<Lib3Dp.State.LocalPrintJob>();
	LocalJobsToSet.Add(localJobs);
	return this;
}

public MachineStateUpdate RemoveLocalJobs(Lib3Dp.State.LocalPrintJob localJobs)
{
	LocalJobsToRemove ??= new HashSet<Lib3Dp.State.LocalPrintJob>();
	LocalJobsToRemove.Add(localJobs);
	return this;
}
public MachineStateUpdate UnsetLocalJobs()
{
	LocalJobsToSet = null;
	LocalJobsToRemove = null;
	return this;
}
public MachineStateUpdate SetScheduledPrints(Lib3Dp.State.ScheduledPrint scheduledPrints)
{
	ScheduledPrintsToSet ??= new HashSet<Lib3Dp.State.ScheduledPrint>();
	ScheduledPrintsToSet.Add(scheduledPrints);
	return this;
}

public MachineStateUpdate RemoveScheduledPrints(Lib3Dp.State.ScheduledPrint scheduledPrints)
{
	ScheduledPrintsToRemove ??= new HashSet<Lib3Dp.State.ScheduledPrint>();
	ScheduledPrintsToRemove.Add(scheduledPrints);
	return this;
}
public MachineStateUpdate UnsetScheduledPrints()
{
	ScheduledPrintsToSet = null;
	ScheduledPrintsToRemove = null;
	return this;
}
	public MachineStateUpdate UpdateExtruders(int key, Func<MachineExtruderUpdate, MachineExtruderUpdate> configure)
	{
		ExtrudersToSet ??= new Dictionary<int, MachineExtruderUpdate>();
		if (!ExtrudersToSet.TryGetValue(key, out var u))
		{
			u = new MachineExtruderUpdate().SetNumber(key);
		}
		ExtrudersToSet[key] = configure(u);
		return this;
	}
	public MachineStateUpdate RemoveExtruders(int key)
	{
		ExtrudersToRemove ??= new HashSet<int>();
		ExtrudersToRemove.Add(key);
		return this;
	}
public MachineStateUpdate UnsetExtruders()
{
	ExtrudersToSet = null;
	ExtrudersToRemove = null;
	return this;
}
	public MachineStateUpdate UpdateNozzles(int key, Func<MachineNozzleUpdate, MachineNozzleUpdate> configure)
	{
		NozzlesToSet ??= new Dictionary<int, MachineNozzleUpdate>();
		if (!NozzlesToSet.TryGetValue(key, out var u))
		{
			u = new MachineNozzleUpdate().SetNumber(key);
		}
		NozzlesToSet[key] = configure(u);
		return this;
	}
	public MachineStateUpdate RemoveNozzles(int key)
	{
		NozzlesToRemove ??= new HashSet<int>();
		NozzlesToRemove.Add(key);
		return this;
	}
public MachineStateUpdate UnsetNozzles()
{
	NozzlesToSet = null;
	NozzlesToRemove = null;
	return this;
}
	public MachineStateUpdate UpdateMaterialUnits(string key, Action<MUnitUpdate> configure)
	{
		MaterialUnitsToSet ??= new Dictionary<string, MUnitUpdate>();
		if (!MaterialUnitsToSet.TryGetValue(key, out var u))
		{
			u = new MUnitUpdate().SetID(key);
			MaterialUnitsToSet[key] = u;
		}
		configure(u);
		return this;
	}
	public MachineStateUpdate RemoveMaterialUnits(string key)
	{
		MaterialUnitsToRemove ??= new HashSet<string>();
		MaterialUnitsToRemove.Add(key);
		return this;
	}
public MachineStateUpdate UnsetMaterialUnits()
{
	MaterialUnitsToSet = null;
	MaterialUnitsToRemove = null;
	return this;
}
public MachineStateUpdate SetAirDuctMode(Lib3Dp.State.MachineAirDuctMode value)
{
	AirDuctModeIsSet = true;
	AirDuctMode = value;
	return this;
}

public MachineStateUpdate RemoveAirDuctMode()
{
	AirDuctModeIsSet = true;
	AirDuctMode = default;
	return this;
}
public MachineStateUpdate UnsetAirDuctMode()
{
	AirDuctModeIsSet = false;
	AirDuctMode = default;
	return this;
}
	public MachineStateUpdate SetFans(string key, int val)
	{
		FansToSet ??= new Dictionary<string, int>();		
		FansToSet[key] = val;
		return this;
	}

	public MachineStateUpdate RemoveFans(string key)
	{
		FansToRemove ??= new HashSet<string>();
		FansToRemove.Add(key);
		return this;
	}
public MachineStateUpdate UnsetFans()
{
	FansToSet = null;
	FansToRemove = null;
	return this;
}
	public MachineStateUpdate SetLights(string key, bool val)
	{
		LightsToSet ??= new Dictionary<string, bool>();		
		LightsToSet[key] = val;
		return this;
	}

	public MachineStateUpdate RemoveLights(string key)
	{
		LightsToRemove ??= new HashSet<string>();
		LightsToRemove.Add(key);
		return this;
	}
public MachineStateUpdate UnsetLights()
{
	LightsToSet = null;
	LightsToRemove = null;
	return this;
}
	public MachineStateUpdate SetHeatingElements(string key, Lib3Dp.State.HeatingElement val)
	{
		HeatingElementsToSet ??= new Dictionary<string, Lib3Dp.State.HeatingElement>();		
		HeatingElementsToSet[key] = val;
		return this;
	}

	public MachineStateUpdate RemoveHeatingElements(string key)
	{
		HeatingElementsToRemove ??= new HashSet<string>();
		HeatingElementsToRemove.Add(key);
		return this;
	}
public MachineStateUpdate UnsetHeatingElements()
{
	HeatingElementsToSet = null;
	HeatingElementsToRemove = null;
	return this;
}
public MachineStateUpdate SetStreamingOMEURL(string value)
{
	StreamingOMEURLIsSet = true;
	StreamingOMEURL = value;
	return this;
}

public MachineStateUpdate RemoveStreamingOMEURL()
{
	StreamingOMEURLIsSet = true;
	StreamingOMEURL = default;
	return this;
}
public MachineStateUpdate UnsetStreamingOMEURL()
{
	StreamingOMEURLIsSet = false;
	StreamingOMEURL = default;
	return this;
}
public MachineStateUpdate SetThumbnailOMEURL(string value)
{
	ThumbnailOMEURLIsSet = true;
	ThumbnailOMEURL = value;
	return this;
}

public MachineStateUpdate RemoveThumbnailOMEURL()
{
	ThumbnailOMEURLIsSet = true;
	ThumbnailOMEURL = default;
	return this;
}
public MachineStateUpdate UnsetThumbnailOMEURL()
{
	ThumbnailOMEURLIsSet = false;
	ThumbnailOMEURL = default;
	return this;
}
	public MachineStateUpdate UpdateNotifications(Lib3Dp.State.MachineMessage key, Action<NotificationUpdate> configure)
	{
		NotificationsToSet ??= new Dictionary<Lib3Dp.State.MachineMessage, NotificationUpdate>();
		if (!NotificationsToSet.TryGetValue(key, out var u))
		{
			u = new NotificationUpdate();
			NotificationsToSet[key] = u;
		}
		configure(u);
		return this;
	}
	public MachineStateUpdate RemoveNotifications(Lib3Dp.State.MachineMessage key)
	{
		NotificationsToRemove ??= new HashSet<Lib3Dp.State.MachineMessage>();
		NotificationsToRemove.Add(key);
		return this;
	}
public MachineStateUpdate UnsetNotifications()
{
	NotificationsToSet = null;
	NotificationsToRemove = null;
	return this;
}
    public bool TryCreate(out MachineState? outResult)
    {
		outResult = null;
        if (!IDIsSet) return false;
        if (!CapabilitiesIsSet) return false;
        if (!StatusIsSet) return false;
        if (!AirDuctModeIsSet) return false;
        var result = new MachineState() { ID = this.ID, Capabilities = this.Capabilities, Status = this.Status, AirDuctMode = this.AirDuctMode };
        AppendUpdate(result, out _);
		outResult = result;

        return true;
    }
    public MachineStateChanges Changes(MachineState machineState)
    {
		if (machineState == null) return default;
		var __ID_hasChanged = false;
		string? __ID_prev = null;
		string? __ID_new = null;
		if (this.IDIsSet)
		{
			if (!EqualityComparer<string>.Default.Equals(machineState.ID, this.ID))
			{
				__ID_hasChanged = true;
				__ID_prev = machineState.ID;
				__ID_new = this.ID;
			}
		}

		var __Brand_hasChanged = false;
		string? __Brand_prev = null;
		string? __Brand_new = null;
		if (this.BrandIsSet)
		{
			if (!EqualityComparer<string>.Default.Equals(machineState.Brand, this.Brand))
			{
				__Brand_hasChanged = true;
				__Brand_prev = machineState.Brand;
				__Brand_new = this.Brand;
			}
		}

		var __Model_hasChanged = false;
		string? __Model_prev = null;
		string? __Model_new = null;
		if (this.ModelIsSet)
		{
			if (!EqualityComparer<string>.Default.Equals(machineState.Model, this.Model))
			{
				__Model_hasChanged = true;
				__Model_prev = machineState.Model;
				__Model_new = this.Model;
			}
		}

		var __Nickname_hasChanged = false;
		string? __Nickname_prev = null;
		string? __Nickname_new = null;
		if (this.NicknameIsSet)
		{
			if (!EqualityComparer<string>.Default.Equals(machineState.Nickname, this.Nickname))
			{
				__Nickname_hasChanged = true;
				__Nickname_prev = machineState.Nickname;
				__Nickname_new = this.Nickname;
			}
		}

		var __Capabilities_hasChanged = false;
		Lib3Dp.State.MachineCapabilities? __Capabilities_prev = null;
		Lib3Dp.State.MachineCapabilities? __Capabilities_new = null;
		if (this.CapabilitiesIsSet)
		{
			if (!EqualityComparer<Lib3Dp.State.MachineCapabilities>.Default.Equals(machineState.Capabilities, this.Capabilities))
			{
				__Capabilities_hasChanged = true;
				__Capabilities_prev = machineState.Capabilities;
				__Capabilities_new = this.Capabilities;
			}
		}

		var __Status_hasChanged = false;
		Lib3Dp.State.MachineStatus? __Status_prev = null;
		Lib3Dp.State.MachineStatus? __Status_new = null;
		if (this.StatusIsSet)
		{
			if (!EqualityComparer<Lib3Dp.State.MachineStatus>.Default.Equals(machineState.Status, this.Status))
			{
				__Status_hasChanged = true;
				__Status_prev = machineState.Status;
				__Status_new = this.Status;
			}
		}

		PrintJobChanges? __CurrentJob_changes = null;
		if (this.CurrentJob != null)
		{
			var __nested_CurrentJob = this.CurrentJob.Changes(machineState.CurrentJob);
			if (__nested_CurrentJob.HasChanged) __CurrentJob_changes = __nested_CurrentJob;
		}

		var __JobHistory_added = new List<Lib3Dp.State.HistoricPrintJob>();
		var __JobHistory_removed = new List<Lib3Dp.State.HistoricPrintJob>();
		if (this.JobHistoryToSet != null)
		{
			foreach (var v in this.JobHistoryToSet)
			{
				if (machineState.JobHistory == null || !machineState.JobHistory.Contains(v)) __JobHistory_added.Add(v);
			}
		}
		if (this.JobHistoryToRemove != null && machineState.JobHistory != null)
		{
			foreach (var v in this.JobHistoryToRemove)
			{
				if (machineState.JobHistory.Contains(v)) __JobHistory_removed.Add(v);
			}
		}

		var __LocalJobs_added = new List<Lib3Dp.State.LocalPrintJob>();
		var __LocalJobs_removed = new List<Lib3Dp.State.LocalPrintJob>();
		if (this.LocalJobsToSet != null)
		{
			foreach (var v in this.LocalJobsToSet)
			{
				if (machineState.LocalJobs == null || !machineState.LocalJobs.Contains(v)) __LocalJobs_added.Add(v);
			}
		}
		if (this.LocalJobsToRemove != null && machineState.LocalJobs != null)
		{
			foreach (var v in this.LocalJobsToRemove)
			{
				if (machineState.LocalJobs.Contains(v)) __LocalJobs_removed.Add(v);
			}
		}

		var __ScheduledPrints_added = new List<Lib3Dp.State.ScheduledPrint>();
		var __ScheduledPrints_removed = new List<Lib3Dp.State.ScheduledPrint>();
		if (this.ScheduledPrintsToSet != null)
		{
			foreach (var v in this.ScheduledPrintsToSet)
			{
				if (machineState.ScheduledPrints == null || !machineState.ScheduledPrints.Contains(v)) __ScheduledPrints_added.Add(v);
			}
		}
		if (this.ScheduledPrintsToRemove != null && machineState.ScheduledPrints != null)
		{
			foreach (var v in this.ScheduledPrintsToRemove)
			{
				if (machineState.ScheduledPrints.Contains(v)) __ScheduledPrints_removed.Add(v);
			}
		}

		var aExtruders_added = new List<KeyValuePair<int, Lib3Dp.State.MachineExtruder>>();
		var uExtruders_updated = new List<KeyValuePair<int, MachineExtruderChanges>>();
		var rExtruders_removed = new List<int>();
		if (this.ExtrudersToSet != null)
		{
			foreach (var kv in this.ExtrudersToSet)
			{
				kv.Value.SetNumber(kv.Key);
				if (kv.Value.TryCreate(out var created))
				{
					if (machineState.Extruders != null && machineState.Extruders.TryGetValue(kv.Key, out var existing))
					{
						if (!EqualityComparer<Lib3Dp.State.MachineExtruder>.Default.Equals(existing, created))
						{
							var __entryChanges = kv.Value.Changes(existing);
							uExtruders_updated.Add(new KeyValuePair<int, MachineExtruderChanges>(kv.Key, __entryChanges));
						}
					}
					else
						aExtruders_added.Add(new KeyValuePair<int, Lib3Dp.State.MachineExtruder>(kv.Key, created));
				}
			}
		}
		if (this.ExtrudersToRemove != null && machineState.Extruders != null)
		{
			foreach (var k in this.ExtrudersToRemove)
			{
				if (machineState.Extruders.ContainsKey(k)) rExtruders_removed.Add(k);
			}
		}

		var aNozzles_added = new List<KeyValuePair<int, Lib3Dp.State.MachineNozzle>>();
		var uNozzles_updated = new List<KeyValuePair<int, MachineNozzleChanges>>();
		var rNozzles_removed = new List<int>();
		if (this.NozzlesToSet != null)
		{
			foreach (var kv in this.NozzlesToSet)
			{
				kv.Value.SetNumber(kv.Key);
				if (kv.Value.TryCreate(out var created))
				{
					if (machineState.Nozzles != null && machineState.Nozzles.TryGetValue(kv.Key, out var existing))
					{
						if (!EqualityComparer<Lib3Dp.State.MachineNozzle>.Default.Equals(existing, created))
						{
							var __entryChanges = kv.Value.Changes(existing);
							uNozzles_updated.Add(new KeyValuePair<int, MachineNozzleChanges>(kv.Key, __entryChanges));
						}
					}
					else
						aNozzles_added.Add(new KeyValuePair<int, Lib3Dp.State.MachineNozzle>(kv.Key, created));
				}
			}
		}
		if (this.NozzlesToRemove != null && machineState.Nozzles != null)
		{
			foreach (var k in this.NozzlesToRemove)
			{
				if (machineState.Nozzles.ContainsKey(k)) rNozzles_removed.Add(k);
			}
		}

		var aMaterialUnits_added = new List<KeyValuePair<string, Lib3Dp.State.MUnit>>();
		var uMaterialUnits_updated = new List<KeyValuePair<string, MUnitChanges>>();
		var rMaterialUnits_removed = new List<string>();
		if (this.MaterialUnitsToSet != null)
		{
			foreach (var kv in this.MaterialUnitsToSet)
			{
				kv.Value.SetID(kv.Key);
				if (kv.Value.TryCreate(out var created))
				{
					if (machineState.MaterialUnits != null && machineState.MaterialUnits.TryGetValue(kv.Key, out var existing))
					{
						if (!EqualityComparer<Lib3Dp.State.MUnit>.Default.Equals(existing, created))
						{
							var __entryChanges = kv.Value.Changes(existing);
							uMaterialUnits_updated.Add(new KeyValuePair<string, MUnitChanges>(kv.Key, __entryChanges));
						}
					}
					else
						aMaterialUnits_added.Add(new KeyValuePair<string, Lib3Dp.State.MUnit>(kv.Key, created));
				}
			}
		}
		if (this.MaterialUnitsToRemove != null && machineState.MaterialUnits != null)
		{
			foreach (var k in this.MaterialUnitsToRemove)
			{
				if (machineState.MaterialUnits.ContainsKey(k)) rMaterialUnits_removed.Add(k);
			}
		}

		var __AirDuctMode_hasChanged = false;
		Lib3Dp.State.MachineAirDuctMode? __AirDuctMode_prev = null;
		Lib3Dp.State.MachineAirDuctMode? __AirDuctMode_new = null;
		if (this.AirDuctModeIsSet)
		{
			if (!EqualityComparer<Lib3Dp.State.MachineAirDuctMode>.Default.Equals(machineState.AirDuctMode, this.AirDuctMode))
			{
				__AirDuctMode_hasChanged = true;
				__AirDuctMode_prev = machineState.AirDuctMode;
				__AirDuctMode_new = this.AirDuctMode;
			}
		}

		var aFans_added = new List<KeyValuePair<string, int>>();
		var uFans_updated = new List<KeyValuePair<string, int>>();
		var rFans_removed = new List<string>();
		if (this.FansToSet != null)
		{
			foreach (var kv in this.FansToSet)
			{
				if (machineState.Fans != null && machineState.Fans.TryGetValue(kv.Key, out var existing))
				{
					if (!EqualityComparer<int>.Default.Equals(existing, kv.Value))
						uFans_updated.Add(kv);
				}
				else
					aFans_added.Add(kv);
			}
		}
		if (this.FansToRemove != null && machineState.Fans != null)
		{
			foreach (var k in this.FansToRemove)
			{
				if (machineState.Fans.ContainsKey(k)) rFans_removed.Add(k);
			}
		}

		var aLights_added = new List<KeyValuePair<string, bool>>();
		var uLights_updated = new List<KeyValuePair<string, bool>>();
		var rLights_removed = new List<string>();
		if (this.LightsToSet != null)
		{
			foreach (var kv in this.LightsToSet)
			{
				if (machineState.Lights != null && machineState.Lights.TryGetValue(kv.Key, out var existing))
				{
					if (!EqualityComparer<bool>.Default.Equals(existing, kv.Value))
						uLights_updated.Add(kv);
				}
				else
					aLights_added.Add(kv);
			}
		}
		if (this.LightsToRemove != null && machineState.Lights != null)
		{
			foreach (var k in this.LightsToRemove)
			{
				if (machineState.Lights.ContainsKey(k)) rLights_removed.Add(k);
			}
		}

		var aHeatingElements_added = new List<KeyValuePair<string, Lib3Dp.State.HeatingElement>>();
		var uHeatingElements_updated = new List<KeyValuePair<string, Lib3Dp.State.HeatingElement>>();
		var rHeatingElements_removed = new List<string>();
		if (this.HeatingElementsToSet != null)
		{
			foreach (var kv in this.HeatingElementsToSet)
			{
				if (machineState.HeatingElements != null && machineState.HeatingElements.TryGetValue(kv.Key, out var existing))
				{
					if (!EqualityComparer<Lib3Dp.State.HeatingElement>.Default.Equals(existing, kv.Value))
						uHeatingElements_updated.Add(kv);
				}
				else
					aHeatingElements_added.Add(kv);
			}
		}
		if (this.HeatingElementsToRemove != null && machineState.HeatingElements != null)
		{
			foreach (var k in this.HeatingElementsToRemove)
			{
				if (machineState.HeatingElements.ContainsKey(k)) rHeatingElements_removed.Add(k);
			}
		}

		var __StreamingOMEURL_hasChanged = false;
		string? __StreamingOMEURL_prev = null;
		string? __StreamingOMEURL_new = null;
		if (this.StreamingOMEURLIsSet)
		{
			if (!EqualityComparer<string>.Default.Equals(machineState.StreamingOMEURL, this.StreamingOMEURL))
			{
				__StreamingOMEURL_hasChanged = true;
				__StreamingOMEURL_prev = machineState.StreamingOMEURL;
				__StreamingOMEURL_new = this.StreamingOMEURL;
			}
		}

		var __ThumbnailOMEURL_hasChanged = false;
		string? __ThumbnailOMEURL_prev = null;
		string? __ThumbnailOMEURL_new = null;
		if (this.ThumbnailOMEURLIsSet)
		{
			if (!EqualityComparer<string>.Default.Equals(machineState.ThumbnailOMEURL, this.ThumbnailOMEURL))
			{
				__ThumbnailOMEURL_hasChanged = true;
				__ThumbnailOMEURL_prev = machineState.ThumbnailOMEURL;
				__ThumbnailOMEURL_new = this.ThumbnailOMEURL;
			}
		}

		var aNotifications_added = new List<KeyValuePair<Lib3Dp.State.MachineMessage, Lib3Dp.State.Notification>>();
		var uNotifications_updated = new List<KeyValuePair<Lib3Dp.State.MachineMessage, NotificationChanges>>();
		var rNotifications_removed = new List<Lib3Dp.State.MachineMessage>();
		if (this.NotificationsToSet != null)
		{
			foreach (var kv in this.NotificationsToSet)
			{
				if (kv.Value.TryCreate(out var created))
				{
					if (machineState.Notifications != null && machineState.Notifications.TryGetValue(kv.Key, out var existing))
					{
						if (!EqualityComparer<Lib3Dp.State.Notification>.Default.Equals(existing, created))
						{
							var __entryChanges = kv.Value.Changes(existing);
							uNotifications_updated.Add(new KeyValuePair<Lib3Dp.State.MachineMessage, NotificationChanges>(kv.Key, __entryChanges));
						}
					}
					else
						aNotifications_added.Add(new KeyValuePair<Lib3Dp.State.MachineMessage, Lib3Dp.State.Notification>(kv.Key, created));
				}
			}
		}
		if (this.NotificationsToRemove != null && machineState.Notifications != null)
		{
			foreach (var k in this.NotificationsToRemove)
			{
				if (machineState.Notifications.ContainsKey(k)) rNotifications_removed.Add(k);
			}
		}

		return new MachineStateChanges(__ID_hasChanged, __ID_prev, __ID_new, __Brand_hasChanged, __Brand_prev, __Brand_new, __Model_hasChanged, __Model_prev, __Model_new, __Nickname_hasChanged, __Nickname_prev, __Nickname_new, __Capabilities_hasChanged, __Capabilities_prev, __Capabilities_new, __Status_hasChanged, __Status_prev, __Status_new, __CurrentJob_changes, __JobHistory_added.ToArray(), __JobHistory_removed.ToArray(), __LocalJobs_added.ToArray(), __LocalJobs_removed.ToArray(), __ScheduledPrints_added.ToArray(), __ScheduledPrints_removed.ToArray(), aExtruders_added.ToArray(), rExtruders_removed.ToArray(), uExtruders_updated.ToArray(), aNozzles_added.ToArray(), rNozzles_removed.ToArray(), uNozzles_updated.ToArray(), aMaterialUnits_added.ToArray(), rMaterialUnits_removed.ToArray(), uMaterialUnits_updated.ToArray(), __AirDuctMode_hasChanged, __AirDuctMode_prev, __AirDuctMode_new, aFans_added.ToArray(), rFans_removed.ToArray(), uFans_updated.ToArray(), aLights_added.ToArray(), rLights_removed.ToArray(), uLights_updated.ToArray(), aHeatingElements_added.ToArray(), rHeatingElements_removed.ToArray(), uHeatingElements_updated.ToArray(), __StreamingOMEURL_hasChanged, __StreamingOMEURL_prev, __StreamingOMEURL_new, __ThumbnailOMEURL_hasChanged, __ThumbnailOMEURL_prev, __ThumbnailOMEURL_new, aNotifications_added.ToArray(), rNotifications_removed.ToArray(), uNotifications_updated.ToArray());
    }

    public void AppendUpdate(MachineState machineState, out MachineStateChanges changes)
    {
		changes = Changes(machineState);

		if (this.IDIsSet)
		{
			machineState.ID = this.ID;
		}

		if (this.BrandIsSet)
		{
			machineState.Brand = this.Brand;
		}

		if (this.ModelIsSet)
		{
			machineState.Model = this.Model;
		}

		if (this.NicknameIsSet)
		{
			machineState.Nickname = this.Nickname;
		}

		if (this.CapabilitiesIsSet)
		{
			machineState.Capabilities = this.Capabilities;
		}

		if (this.StatusIsSet)
		{
			machineState.Status = this.Status;
		}

		if (this.CurrentJobIsSet)
		{
			if (this.CurrentJob.TryCreate(out var createdCurrentJob))
				machineState.CurrentJob = createdCurrentJob;
		}

		if (this.JobHistoryToSet != null)
		{
			machineState.JobHistory.UnionWith(this.JobHistoryToSet);
		}
		if (this.JobHistoryToRemove != null && machineState.JobHistory != null)
		{
			machineState.JobHistory.ExceptWith(this.JobHistoryToRemove);
		}

		if (this.LocalJobsToSet != null)
		{
			machineState.LocalJobs.UnionWith(this.LocalJobsToSet);
		}
		if (this.LocalJobsToRemove != null && machineState.LocalJobs != null)
		{
			machineState.LocalJobs.ExceptWith(this.LocalJobsToRemove);
		}

		if (this.ScheduledPrintsToSet != null)
		{
			machineState.ScheduledPrints.UnionWith(this.ScheduledPrintsToSet);
		}
		if (this.ScheduledPrintsToRemove != null && machineState.ScheduledPrints != null)
		{
			machineState.ScheduledPrints.ExceptWith(this.ScheduledPrintsToRemove);
		}

		if (this.ExtrudersToSet != null)
		{
			foreach (var kv in this.ExtrudersToSet)
			{
				kv.Value.SetNumber(kv.Key);
				if (machineState.Extruders != null && machineState.Extruders.TryGetValue(kv.Key, out var __existing_Extruders))
				{
					kv.Value.AppendUpdate(ref __existing_Extruders, out _);
					machineState.Extruders[kv.Key] = __existing_Extruders;
				}
				else if (kv.Value.TryCreate(out var __created_Extruders))
					machineState.Extruders[kv.Key] = __created_Extruders;
			}
		}
		if (this.ExtrudersToRemove != null && machineState.Extruders != null)
		{
			foreach (var k in this.ExtrudersToRemove) machineState.Extruders.Remove(k);
		}

		if (this.NozzlesToSet != null)
		{
			foreach (var kv in this.NozzlesToSet)
			{
				kv.Value.SetNumber(kv.Key);
				if (machineState.Nozzles != null && machineState.Nozzles.TryGetValue(kv.Key, out var __existing_Nozzles))
				{
					kv.Value.AppendUpdate(ref __existing_Nozzles, out _);
					machineState.Nozzles[kv.Key] = __existing_Nozzles;
				}
				else if (kv.Value.TryCreate(out var __created_Nozzles))
					machineState.Nozzles[kv.Key] = __created_Nozzles;
			}
		}
		if (this.NozzlesToRemove != null && machineState.Nozzles != null)
		{
			foreach (var k in this.NozzlesToRemove) machineState.Nozzles.Remove(k);
		}

		if (this.MaterialUnitsToSet != null)
		{
			foreach (var kv in this.MaterialUnitsToSet)
			{
				kv.Value.SetID(kv.Key);
				if (machineState.MaterialUnits != null && machineState.MaterialUnits.TryGetValue(kv.Key, out var __existing_MaterialUnits))
					kv.Value.AppendUpdate(__existing_MaterialUnits, out _);
				else if (kv.Value.TryCreate(out var __created_MaterialUnits))
					machineState.MaterialUnits[kv.Key] = __created_MaterialUnits;
			}
		}
		if (this.MaterialUnitsToRemove != null && machineState.MaterialUnits != null)
		{
			foreach (var k in this.MaterialUnitsToRemove) machineState.MaterialUnits.Remove(k);
		}

		if (this.AirDuctModeIsSet)
		{
			machineState.AirDuctMode = this.AirDuctMode;
		}

		if (this.FansToSet != null)
		{
			foreach (var kv in this.FansToSet)
				machineState.Fans[kv.Key] = kv.Value;
		}
		if (this.FansToRemove != null && machineState.Fans != null)
		{
			foreach (var k in this.FansToRemove) machineState.Fans.Remove(k);
		}

		if (this.LightsToSet != null)
		{
			foreach (var kv in this.LightsToSet)
				machineState.Lights[kv.Key] = kv.Value;
		}
		if (this.LightsToRemove != null && machineState.Lights != null)
		{
			foreach (var k in this.LightsToRemove) machineState.Lights.Remove(k);
		}

		if (this.HeatingElementsToSet != null)
		{
			foreach (var kv in this.HeatingElementsToSet)
				machineState.HeatingElements[kv.Key] = kv.Value;
		}
		if (this.HeatingElementsToRemove != null && machineState.HeatingElements != null)
		{
			foreach (var k in this.HeatingElementsToRemove) machineState.HeatingElements.Remove(k);
		}

		if (this.StreamingOMEURLIsSet)
		{
			machineState.StreamingOMEURL = this.StreamingOMEURL;
		}

		if (this.ThumbnailOMEURLIsSet)
		{
			machineState.ThumbnailOMEURL = this.ThumbnailOMEURL;
		}

		if (this.NotificationsToSet != null)
		{
			foreach (var kv in this.NotificationsToSet)
			{
				if (machineState.Notifications != null && machineState.Notifications.TryGetValue(kv.Key, out var __existing_Notifications))
					kv.Value.AppendUpdate(__existing_Notifications, out _);
				else if (kv.Value.TryCreate(out var __created_Notifications))
					machineState.Notifications[kv.Key] = __created_Notifications;
			}
		}
		if (this.NotificationsToRemove != null && machineState.Notifications != null)
		{
			foreach (var k in this.NotificationsToRemove) machineState.Notifications.Remove(k);
		}

    }

}
