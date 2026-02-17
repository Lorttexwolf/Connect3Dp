namespace Lib3Dp.State
{
	public readonly record struct HeatingElement(double TempC, double TargetTempC, HeatingConstraints Constraints);
}
