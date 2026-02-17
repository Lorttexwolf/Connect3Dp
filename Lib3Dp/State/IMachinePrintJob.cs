namespace Lib3Dp.State
{
	public interface IMachinePrintJob
	{
		string Name { get; }
		int PercentageComplete { get; }
		TimeSpan RemainingTime { get; }
		TimeSpan TotalTime { get; }
		MachineMessage? Issue { get; }
		MachineFileHandle? File { get; }
		MachineFileHandle? Thumbnail { get; }
		string? SubStage { get; }
		public int? TotalMaterialUsage { get; set; }

		/// <summary>
		/// Maps each material source used by this print job to the amount consumed in grams.
		/// </summary>
		IReadOnlyDictionary<SpoolLocation, int>? MaterialUsages { get; }
	}
}
