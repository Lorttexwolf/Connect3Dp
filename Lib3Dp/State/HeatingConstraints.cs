namespace Lib3Dp.State
{
	public record struct HeatingConstraints(int MinTempC, int MaxTempC)
	{
		public override readonly string ToString()
		{
			return $"{MinTempC} C -> {MaxTempC} C";
		}
	}
}
