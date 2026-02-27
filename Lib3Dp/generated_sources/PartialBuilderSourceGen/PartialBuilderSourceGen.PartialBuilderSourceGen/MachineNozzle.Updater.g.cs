#nullable enable
namespace Lib3Dp.State;

public struct MachineNozzleUpdate
{
    public int Number { get; private set; }
    public bool NumberIsSet { get; private set; }
    public double Diameter { get; private set; }
    public bool DiameterIsSet { get; private set; }

public MachineNozzleUpdate SetNumber(int value)
{
	NumberIsSet = true;
	Number = value;
	return this;
}

public MachineNozzleUpdate RemoveNumber()
{
	NumberIsSet = true;
	Number = default;
	return this;
}
public MachineNozzleUpdate UnsetNumber()
{
	NumberIsSet = false;
	Number = default;
	return this;
}
public MachineNozzleUpdate SetDiameter(double value)
{
	DiameterIsSet = true;
	Diameter = value;
	return this;
}

public MachineNozzleUpdate RemoveDiameter()
{
	DiameterIsSet = true;
	Diameter = default;
	return this;
}
public MachineNozzleUpdate UnsetDiameter()
{
	DiameterIsSet = false;
	Diameter = default;
	return this;
}
    public bool TryCreate(out MachineNozzle outResult)
    {
		outResult = default;
        if (!NumberIsSet) return false;
        if (!DiameterIsSet) return false;
        var result = new MachineNozzle() { Number = this.Number, Diameter = this.Diameter };
        AppendUpdate(ref result, out _);
		outResult = result;

        return true;
    }
    public MachineNozzleChanges Changes(in MachineNozzle machineNozzle)
    {
		var __Number_hasChanged = false;
		int? __Number_prev = null;
		int? __Number_new = null;
		if (this.NumberIsSet)
		{
			if (!EqualityComparer<int>.Default.Equals(machineNozzle.Number, this.Number))
			{
				__Number_hasChanged = true;
				__Number_prev = machineNozzle.Number;
				__Number_new = this.Number;
			}
		}

		var __Diameter_hasChanged = false;
		double? __Diameter_prev = null;
		double? __Diameter_new = null;
		if (this.DiameterIsSet)
		{
			if (!EqualityComparer<double>.Default.Equals(machineNozzle.Diameter, this.Diameter))
			{
				__Diameter_hasChanged = true;
				__Diameter_prev = machineNozzle.Diameter;
				__Diameter_new = this.Diameter;
			}
		}

		return new MachineNozzleChanges(__Number_hasChanged, __Number_prev, __Number_new, __Diameter_hasChanged, __Diameter_prev, __Diameter_new);
    }

    public void AppendUpdate(ref MachineNozzle machineNozzle, out MachineNozzleChanges changes)
    {
		changes = Changes(in machineNozzle);

		if (this.NumberIsSet)
		{
			machineNozzle.Number = this.Number;
		}

		if (this.DiameterIsSet)
		{
			machineNozzle.Diameter = this.Diameter;
		}

    }

}
