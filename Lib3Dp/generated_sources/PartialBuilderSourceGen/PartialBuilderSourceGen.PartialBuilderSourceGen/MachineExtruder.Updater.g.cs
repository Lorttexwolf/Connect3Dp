#nullable enable
namespace Lib3Dp.State;

public struct MachineExtruderUpdate
{
    public int Number { get; private set; }
    public bool NumberIsSet { get; private set; }
    public Lib3Dp.State.HeatingConstraints HeatingConstraint { get; private set; }
    public bool HeatingConstraintIsSet { get; private set; }
    public double TempC { get; private set; }
    public bool TempCIsSet { get; private set; }
    public double? TargetTempC { get; private set; }
    public bool TargetTempCIsSet { get; private set; }
    public int? NozzleNumber { get; private set; }
    public bool NozzleNumberIsSet { get; private set; }
    public Lib3Dp.State.SpoolLocation? LoadedSpool { get; private set; }
    public bool LoadedSpoolIsSet { get; private set; }

public MachineExtruderUpdate SetNumber(int value)
{
	NumberIsSet = true;
	Number = value;
	return this;
}

public MachineExtruderUpdate RemoveNumber()
{
	NumberIsSet = true;
	Number = default;
	return this;
}
public MachineExtruderUpdate UnsetNumber()
{
	NumberIsSet = false;
	Number = default;
	return this;
}
public MachineExtruderUpdate SetHeatingConstraint(Lib3Dp.State.HeatingConstraints value)
{
	HeatingConstraintIsSet = true;
	HeatingConstraint = value;
	return this;
}

public MachineExtruderUpdate RemoveHeatingConstraint()
{
	HeatingConstraintIsSet = true;
	HeatingConstraint = default;
	return this;
}
public MachineExtruderUpdate UnsetHeatingConstraint()
{
	HeatingConstraintIsSet = false;
	HeatingConstraint = default;
	return this;
}
public MachineExtruderUpdate SetTempC(double value)
{
	TempCIsSet = true;
	TempC = value;
	return this;
}

public MachineExtruderUpdate RemoveTempC()
{
	TempCIsSet = true;
	TempC = default;
	return this;
}
public MachineExtruderUpdate UnsetTempC()
{
	TempCIsSet = false;
	TempC = default;
	return this;
}
public MachineExtruderUpdate SetTargetTempC(double? value)
{
	TargetTempCIsSet = true;
	TargetTempC = value;
	return this;
}

public MachineExtruderUpdate RemoveTargetTempC()
{
	TargetTempCIsSet = true;
	TargetTempC = default;
	return this;
}
public MachineExtruderUpdate UnsetTargetTempC()
{
	TargetTempCIsSet = false;
	TargetTempC = default;
	return this;
}
public MachineExtruderUpdate SetNozzleNumber(int? value)
{
	NozzleNumberIsSet = true;
	NozzleNumber = value;
	return this;
}

public MachineExtruderUpdate RemoveNozzleNumber()
{
	NozzleNumberIsSet = true;
	NozzleNumber = default;
	return this;
}
public MachineExtruderUpdate UnsetNozzleNumber()
{
	NozzleNumberIsSet = false;
	NozzleNumber = default;
	return this;
}
public MachineExtruderUpdate SetLoadedSpool(Lib3Dp.State.SpoolLocation? value)
{
	LoadedSpoolIsSet = true;
	LoadedSpool = value;
	return this;
}

public MachineExtruderUpdate RemoveLoadedSpool()
{
	LoadedSpoolIsSet = true;
	LoadedSpool = default;
	return this;
}
public MachineExtruderUpdate UnsetLoadedSpool()
{
	LoadedSpoolIsSet = false;
	LoadedSpool = default;
	return this;
}
    public bool TryCreate(out MachineExtruder outResult)
    {
		outResult = default;
        if (!NumberIsSet) return false;
        if (!HeatingConstraintIsSet) return false;
        if (!TempCIsSet) return false;
        var result = new MachineExtruder() { Number = this.Number, HeatingConstraint = this.HeatingConstraint, TempC = this.TempC };
        AppendUpdate(ref result, out _);
		outResult = result;

        return true;
    }
    public MachineExtruderChanges Changes(in MachineExtruder machineExtruder)
    {
		var __Number_hasChanged = false;
		int? __Number_prev = null;
		int? __Number_new = null;
		if (this.NumberIsSet)
		{
			if (!EqualityComparer<int>.Default.Equals(machineExtruder.Number, this.Number))
			{
				__Number_hasChanged = true;
				__Number_prev = machineExtruder.Number;
				__Number_new = this.Number;
			}
		}

		var __HeatingConstraint_hasChanged = false;
		Lib3Dp.State.HeatingConstraints? __HeatingConstraint_prev = null;
		Lib3Dp.State.HeatingConstraints? __HeatingConstraint_new = null;
		if (this.HeatingConstraintIsSet)
		{
			if (!EqualityComparer<Lib3Dp.State.HeatingConstraints>.Default.Equals(machineExtruder.HeatingConstraint, this.HeatingConstraint))
			{
				__HeatingConstraint_hasChanged = true;
				__HeatingConstraint_prev = machineExtruder.HeatingConstraint;
				__HeatingConstraint_new = this.HeatingConstraint;
			}
		}

		var __TempC_hasChanged = false;
		double? __TempC_prev = null;
		double? __TempC_new = null;
		if (this.TempCIsSet)
		{
			if (!EqualityComparer<double>.Default.Equals(machineExtruder.TempC, this.TempC))
			{
				__TempC_hasChanged = true;
				__TempC_prev = machineExtruder.TempC;
				__TempC_new = this.TempC;
			}
		}

		var __TargetTempC_hasChanged = false;
		double? __TargetTempC_prev = null;
		double? __TargetTempC_new = null;
		if (this.TargetTempCIsSet)
		{
			if (!EqualityComparer<double?>.Default.Equals(machineExtruder.TargetTempC, this.TargetTempC))
			{
				__TargetTempC_hasChanged = true;
				__TargetTempC_prev = machineExtruder.TargetTempC;
				__TargetTempC_new = this.TargetTempC;
			}
		}

		var __NozzleNumber_hasChanged = false;
		int? __NozzleNumber_prev = null;
		int? __NozzleNumber_new = null;
		if (this.NozzleNumberIsSet)
		{
			if (!EqualityComparer<int?>.Default.Equals(machineExtruder.NozzleNumber, this.NozzleNumber))
			{
				__NozzleNumber_hasChanged = true;
				__NozzleNumber_prev = machineExtruder.NozzleNumber;
				__NozzleNumber_new = this.NozzleNumber;
			}
		}

		var __LoadedSpool_hasChanged = false;
		Lib3Dp.State.SpoolLocation? __LoadedSpool_prev = null;
		Lib3Dp.State.SpoolLocation? __LoadedSpool_new = null;
		if (this.LoadedSpoolIsSet)
		{
			if (!EqualityComparer<Lib3Dp.State.SpoolLocation?>.Default.Equals(machineExtruder.LoadedSpool, this.LoadedSpool))
			{
				__LoadedSpool_hasChanged = true;
				__LoadedSpool_prev = machineExtruder.LoadedSpool;
				__LoadedSpool_new = this.LoadedSpool;
			}
		}

		return new MachineExtruderChanges(__Number_hasChanged, __Number_prev, __Number_new, __HeatingConstraint_hasChanged, __HeatingConstraint_prev, __HeatingConstraint_new, __TempC_hasChanged, __TempC_prev, __TempC_new, __TargetTempC_hasChanged, __TargetTempC_prev, __TargetTempC_new, __NozzleNumber_hasChanged, __NozzleNumber_prev, __NozzleNumber_new, __LoadedSpool_hasChanged, __LoadedSpool_prev, __LoadedSpool_new);
    }

    public void AppendUpdate(ref MachineExtruder machineExtruder, out MachineExtruderChanges changes)
    {
		changes = Changes(in machineExtruder);

		if (this.NumberIsSet)
		{
			machineExtruder.Number = this.Number;
		}

		if (this.HeatingConstraintIsSet)
		{
			machineExtruder.HeatingConstraint = this.HeatingConstraint;
		}

		if (this.TempCIsSet)
		{
			machineExtruder.TempC = this.TempC;
		}

		if (this.TargetTempCIsSet)
		{
			machineExtruder.TargetTempC = this.TargetTempC;
		}

		if (this.NozzleNumberIsSet)
		{
			machineExtruder.NozzleNumber = this.NozzleNumber;
		}

		if (this.LoadedSpoolIsSet)
		{
			machineExtruder.LoadedSpool = this.LoadedSpool;
		}

    }

}
