namespace Lib3Dp.State
{
	public readonly record struct HeatingSettings(double TempC, TimeSpan Duration, bool? DoSpin)
	{
		public readonly bool IsInRange(HeatingConstraints constraints)
		{
			return TempC >= constraints.MinTempC && TempC <= constraints.MaxTempC;
		}
	}
}
