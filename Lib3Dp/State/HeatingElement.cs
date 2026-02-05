namespace Lib3Dp.State
{
	public record struct HeatingElement(double TempC, double TargetTempC, HeatingConstraints Constraints);
}
