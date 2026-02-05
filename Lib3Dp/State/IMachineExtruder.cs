namespace Lib3Dp.State
{
	public interface IMachineExtruder : IEquatable<IMachineExtruder>
	{
		int Number { get; }
		HeatingConstraints HeatingConstraint { get; }

		double TempC { get; }
		double? TargetTempC { get; }

		int? NozzleNumber { get; }

		SpoolLocation? LoadedSpool { get; }
	}
}
