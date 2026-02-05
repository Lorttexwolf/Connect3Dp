namespace Lib3Dp.State
{
	public readonly record struct SpoolLocation(string MMID, int Slot)
	{
		public override string ToString()
		{
			return $"MU: {MMID} Slot: {Slot}";
		}
	}
}
