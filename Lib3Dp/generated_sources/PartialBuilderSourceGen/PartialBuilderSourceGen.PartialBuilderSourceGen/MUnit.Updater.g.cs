#nullable enable
namespace Lib3Dp.State;

public sealed class MUnitUpdate
{
    public string ID { get; private set; }
    public bool IDIsSet { get; private set; }
    public int Capacity { get; private set; }
    public bool CapacityIsSet { get; private set; }
    public string Model { get; private set; }
    public bool ModelIsSet { get; private set; }
    public Lib3Dp.State.MUCapabilities Capabilities { get; private set; }
    public bool CapabilitiesIsSet { get; private set; }
    public Lib3Dp.State.HeatingConstraints? HeatingConstraints { get; private set; }
    public bool HeatingConstraintsIsSet { get; private set; }
    public Dictionary<int,SpoolUpdate>? TraysToSet { get; private set; }
    public HashSet<int>? TraysToRemove { get; private set; }
    public double? HumidityPercent { get; private set; }
    public bool HumidityPercentIsSet { get; private set; }
    public double? TemperatureC { get; private set; }
    public bool TemperatureCIsSet { get; private set; }
    public Lib3Dp.State.HeatingJob? HeatingJob { get; private set; }
    public bool HeatingJobIsSet { get; private set; }
    public HashSet<Lib3Dp.State.HeatingSchedule>? HeatingScheduleToSet { get; private set; }
    public HashSet<Lib3Dp.State.HeatingSchedule>? HeatingScheduleToRemove { get; private set; }

public MUnitUpdate SetID(string value)
{
	IDIsSet = true;
	ID = value;
	return this;
}

public MUnitUpdate RemoveID()
{
	IDIsSet = true;
	ID = default;
	return this;
}
public MUnitUpdate UnsetID()
{
	IDIsSet = false;
	ID = default;
	return this;
}
public MUnitUpdate SetCapacity(int value)
{
	CapacityIsSet = true;
	Capacity = value;
	return this;
}

public MUnitUpdate RemoveCapacity()
{
	CapacityIsSet = true;
	Capacity = default;
	return this;
}
public MUnitUpdate UnsetCapacity()
{
	CapacityIsSet = false;
	Capacity = default;
	return this;
}
public MUnitUpdate SetModel(string value)
{
	ModelIsSet = true;
	Model = value;
	return this;
}

public MUnitUpdate RemoveModel()
{
	ModelIsSet = true;
	Model = default;
	return this;
}
public MUnitUpdate UnsetModel()
{
	ModelIsSet = false;
	Model = default;
	return this;
}
public MUnitUpdate SetCapabilities(Lib3Dp.State.MUCapabilities value)
{
	CapabilitiesIsSet = true;
	Capabilities = value;
	return this;
}

public MUnitUpdate RemoveCapabilities()
{
	CapabilitiesIsSet = true;
	Capabilities = default;
	return this;
}
public MUnitUpdate UnsetCapabilities()
{
	CapabilitiesIsSet = false;
	Capabilities = default;
	return this;
}
public MUnitUpdate SetHeatingConstraints(Lib3Dp.State.HeatingConstraints? value)
{
	HeatingConstraintsIsSet = true;
	HeatingConstraints = value;
	return this;
}

public MUnitUpdate RemoveHeatingConstraints()
{
	HeatingConstraintsIsSet = true;
	HeatingConstraints = default;
	return this;
}
public MUnitUpdate UnsetHeatingConstraints()
{
	HeatingConstraintsIsSet = false;
	HeatingConstraints = default;
	return this;
}
	public MUnitUpdate UpdateTrays(int key, Func<SpoolUpdate, SpoolUpdate> configure)
	{
		TraysToSet ??= new Dictionary<int, SpoolUpdate>();
		if (!TraysToSet.TryGetValue(key, out var u))
		{
			u = new SpoolUpdate().SetNumber(key);
		}
		TraysToSet[key] = configure(u);
		return this;
	}
	public MUnitUpdate RemoveTrays(int key)
	{
		TraysToRemove ??= new HashSet<int>();
		TraysToRemove.Add(key);
		return this;
	}
public MUnitUpdate UnsetTrays()
{
	TraysToSet = null;
	TraysToRemove = null;
	return this;
}
public MUnitUpdate SetHumidityPercent(double? value)
{
	HumidityPercentIsSet = true;
	HumidityPercent = value;
	return this;
}

public MUnitUpdate RemoveHumidityPercent()
{
	HumidityPercentIsSet = true;
	HumidityPercent = default;
	return this;
}
public MUnitUpdate UnsetHumidityPercent()
{
	HumidityPercentIsSet = false;
	HumidityPercent = default;
	return this;
}
public MUnitUpdate SetTemperatureC(double? value)
{
	TemperatureCIsSet = true;
	TemperatureC = value;
	return this;
}

public MUnitUpdate RemoveTemperatureC()
{
	TemperatureCIsSet = true;
	TemperatureC = default;
	return this;
}
public MUnitUpdate UnsetTemperatureC()
{
	TemperatureCIsSet = false;
	TemperatureC = default;
	return this;
}
public MUnitUpdate SetHeatingJob(Lib3Dp.State.HeatingJob? value)
{
	HeatingJobIsSet = true;
	HeatingJob = value;
	return this;
}

public MUnitUpdate RemoveHeatingJob()
{
	HeatingJobIsSet = true;
	HeatingJob = default;
	return this;
}
public MUnitUpdate UnsetHeatingJob()
{
	HeatingJobIsSet = false;
	HeatingJob = default;
	return this;
}
public MUnitUpdate SetHeatingSchedule(Lib3Dp.State.HeatingSchedule heatingSchedule)
{
	HeatingScheduleToSet ??= new HashSet<Lib3Dp.State.HeatingSchedule>();
	HeatingScheduleToSet.Add(heatingSchedule);
	return this;
}

public MUnitUpdate RemoveHeatingSchedule(Lib3Dp.State.HeatingSchedule heatingSchedule)
{
	HeatingScheduleToRemove ??= new HashSet<Lib3Dp.State.HeatingSchedule>();
	HeatingScheduleToRemove.Add(heatingSchedule);
	return this;
}
public MUnitUpdate UnsetHeatingSchedule()
{
	HeatingScheduleToSet = null;
	HeatingScheduleToRemove = null;
	return this;
}
    public bool TryCreate(out MUnit? outResult)
    {
		outResult = null;
        if (!IDIsSet) return false;
        if (!CapacityIsSet) return false;
        if (!CapabilitiesIsSet) return false;
        var result = new MUnit() { ID = this.ID, Capacity = this.Capacity, Capabilities = this.Capabilities };
        AppendUpdate(result, out _);
		outResult = result;

        return true;
    }
    public MUnitChanges Changes(MUnit mUnit)
    {
		if (mUnit == null) return default;
		var __ID_hasChanged = false;
		string? __ID_prev = null;
		string? __ID_new = null;
		if (this.IDIsSet)
		{
			if (!EqualityComparer<string>.Default.Equals(mUnit.ID, this.ID))
			{
				__ID_hasChanged = true;
				__ID_prev = mUnit.ID;
				__ID_new = this.ID;
			}
		}

		var __Capacity_hasChanged = false;
		int? __Capacity_prev = null;
		int? __Capacity_new = null;
		if (this.CapacityIsSet)
		{
			if (!EqualityComparer<int>.Default.Equals(mUnit.Capacity, this.Capacity))
			{
				__Capacity_hasChanged = true;
				__Capacity_prev = mUnit.Capacity;
				__Capacity_new = this.Capacity;
			}
		}

		var __Model_hasChanged = false;
		string? __Model_prev = null;
		string? __Model_new = null;
		if (this.ModelIsSet)
		{
			if (!EqualityComparer<string>.Default.Equals(mUnit.Model, this.Model))
			{
				__Model_hasChanged = true;
				__Model_prev = mUnit.Model;
				__Model_new = this.Model;
			}
		}

		var __Capabilities_hasChanged = false;
		Lib3Dp.State.MUCapabilities? __Capabilities_prev = null;
		Lib3Dp.State.MUCapabilities? __Capabilities_new = null;
		if (this.CapabilitiesIsSet)
		{
			if (!EqualityComparer<Lib3Dp.State.MUCapabilities>.Default.Equals(mUnit.Capabilities, this.Capabilities))
			{
				__Capabilities_hasChanged = true;
				__Capabilities_prev = mUnit.Capabilities;
				__Capabilities_new = this.Capabilities;
			}
		}

		var __HeatingConstraints_hasChanged = false;
		Lib3Dp.State.HeatingConstraints? __HeatingConstraints_prev = null;
		Lib3Dp.State.HeatingConstraints? __HeatingConstraints_new = null;
		if (this.HeatingConstraintsIsSet)
		{
			if (!EqualityComparer<Lib3Dp.State.HeatingConstraints?>.Default.Equals(mUnit.HeatingConstraints, this.HeatingConstraints))
			{
				__HeatingConstraints_hasChanged = true;
				__HeatingConstraints_prev = mUnit.HeatingConstraints;
				__HeatingConstraints_new = this.HeatingConstraints;
			}
		}

		var aTrays_added = new List<KeyValuePair<int, Lib3Dp.State.Spool>>();
		var uTrays_updated = new List<KeyValuePair<int, SpoolChanges>>();
		var rTrays_removed = new List<int>();
		if (this.TraysToSet != null)
		{
			foreach (var kv in this.TraysToSet)
			{
				kv.Value.SetNumber(kv.Key);
				if (kv.Value.TryCreate(out var created))
				{
					if (mUnit.Trays != null && mUnit.Trays.TryGetValue(kv.Key, out var existing))
					{
						if (!EqualityComparer<Lib3Dp.State.Spool>.Default.Equals(existing, created))
						{
							var __entryChanges = kv.Value.Changes(existing);
							uTrays_updated.Add(new KeyValuePair<int, SpoolChanges>(kv.Key, __entryChanges));
						}
					}
					else
						aTrays_added.Add(new KeyValuePair<int, Lib3Dp.State.Spool>(kv.Key, created));
				}
			}
		}
		if (this.TraysToRemove != null && mUnit.Trays != null)
		{
			foreach (var k in this.TraysToRemove)
			{
				if (mUnit.Trays.ContainsKey(k)) rTrays_removed.Add(k);
			}
		}

		var __HumidityPercent_hasChanged = false;
		double? __HumidityPercent_prev = null;
		double? __HumidityPercent_new = null;
		if (this.HumidityPercentIsSet)
		{
			if (!EqualityComparer<double?>.Default.Equals(mUnit.HumidityPercent, this.HumidityPercent))
			{
				__HumidityPercent_hasChanged = true;
				__HumidityPercent_prev = mUnit.HumidityPercent;
				__HumidityPercent_new = this.HumidityPercent;
			}
		}

		var __TemperatureC_hasChanged = false;
		double? __TemperatureC_prev = null;
		double? __TemperatureC_new = null;
		if (this.TemperatureCIsSet)
		{
			if (!EqualityComparer<double?>.Default.Equals(mUnit.TemperatureC, this.TemperatureC))
			{
				__TemperatureC_hasChanged = true;
				__TemperatureC_prev = mUnit.TemperatureC;
				__TemperatureC_new = this.TemperatureC;
			}
		}

		var __HeatingJob_hasChanged = false;
		Lib3Dp.State.HeatingJob? __HeatingJob_prev = null;
		Lib3Dp.State.HeatingJob? __HeatingJob_new = null;
		if (this.HeatingJobIsSet)
		{
			if (!EqualityComparer<Lib3Dp.State.HeatingJob?>.Default.Equals(mUnit.HeatingJob, this.HeatingJob))
			{
				__HeatingJob_hasChanged = true;
				__HeatingJob_prev = mUnit.HeatingJob;
				__HeatingJob_new = this.HeatingJob;
			}
		}

		var __HeatingSchedule_added = new List<Lib3Dp.State.HeatingSchedule>();
		var __HeatingSchedule_removed = new List<Lib3Dp.State.HeatingSchedule>();
		if (this.HeatingScheduleToSet != null)
		{
			foreach (var v in this.HeatingScheduleToSet)
			{
				if (mUnit.HeatingSchedule == null || !mUnit.HeatingSchedule.Contains(v)) __HeatingSchedule_added.Add(v);
			}
		}
		if (this.HeatingScheduleToRemove != null && mUnit.HeatingSchedule != null)
		{
			foreach (var v in this.HeatingScheduleToRemove)
			{
				if (mUnit.HeatingSchedule.Contains(v)) __HeatingSchedule_removed.Add(v);
			}
		}

		return new MUnitChanges(__ID_hasChanged, __ID_prev, __ID_new, __Capacity_hasChanged, __Capacity_prev, __Capacity_new, __Model_hasChanged, __Model_prev, __Model_new, __Capabilities_hasChanged, __Capabilities_prev, __Capabilities_new, __HeatingConstraints_hasChanged, __HeatingConstraints_prev, __HeatingConstraints_new, aTrays_added.ToArray(), rTrays_removed.ToArray(), uTrays_updated.ToArray(), __HumidityPercent_hasChanged, __HumidityPercent_prev, __HumidityPercent_new, __TemperatureC_hasChanged, __TemperatureC_prev, __TemperatureC_new, __HeatingJob_hasChanged, __HeatingJob_prev, __HeatingJob_new, __HeatingSchedule_added.ToArray(), __HeatingSchedule_removed.ToArray());
    }

    public void AppendUpdate(MUnit mUnit, out MUnitChanges changes)
    {
		changes = Changes(mUnit);

		if (this.IDIsSet)
		{
			mUnit.ID = this.ID;
		}

		if (this.CapacityIsSet)
		{
			mUnit.Capacity = this.Capacity;
		}

		if (this.ModelIsSet)
		{
			mUnit.Model = this.Model;
		}

		if (this.CapabilitiesIsSet)
		{
			mUnit.Capabilities = this.Capabilities;
		}

		if (this.HeatingConstraintsIsSet)
		{
			mUnit.HeatingConstraints = this.HeatingConstraints;
		}

		if (this.TraysToSet != null)
		{
			foreach (var kv in this.TraysToSet)
			{
				kv.Value.SetNumber(kv.Key);
				if (mUnit.Trays != null && mUnit.Trays.TryGetValue(kv.Key, out var __existing_Trays))
				{
					kv.Value.AppendUpdate(ref __existing_Trays, out _);
					mUnit.Trays[kv.Key] = __existing_Trays;
				}
				else if (kv.Value.TryCreate(out var __created_Trays))
					mUnit.Trays[kv.Key] = __created_Trays;
			}
		}
		if (this.TraysToRemove != null && mUnit.Trays != null)
		{
			foreach (var k in this.TraysToRemove) mUnit.Trays.Remove(k);
		}

		if (this.HumidityPercentIsSet)
		{
			mUnit.HumidityPercent = this.HumidityPercent;
		}

		if (this.TemperatureCIsSet)
		{
			mUnit.TemperatureC = this.TemperatureC;
		}

		if (this.HeatingJobIsSet)
		{
			mUnit.HeatingJob = this.HeatingJob;
		}

		if (this.HeatingScheduleToSet != null)
		{
			mUnit.HeatingSchedule.UnionWith(this.HeatingScheduleToSet);
		}
		if (this.HeatingScheduleToRemove != null && mUnit.HeatingSchedule != null)
		{
			mUnit.HeatingSchedule.ExceptWith(this.HeatingScheduleToRemove);
		}

    }

}
