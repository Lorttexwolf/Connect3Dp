namespace Lib3Dp.State
{
	public interface IMachineNozzle : IEquatable<IMachineNozzle>
	{
		int Number { get; }
		double Diameter { get; }

	}
}
