namespace Lib3Dp.State
{
	public interface IReadOnlySpool
	{
		int Number { get; }
		Material Material { get; }
		int? GramsMaximum { get; }
		int? GramsRemaining { get; }
	}
}
