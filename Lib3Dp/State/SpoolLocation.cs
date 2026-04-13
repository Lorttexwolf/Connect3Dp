namespace Lib3Dp.State
{
	public readonly record struct SpoolLocation(string MUID, int Slot)
	{
		public override string ToString()
		{
			return $"MU: {MUID} Slot: {Slot}";
		}
	}
}
