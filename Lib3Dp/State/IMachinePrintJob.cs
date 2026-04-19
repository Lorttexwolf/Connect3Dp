using Lib3Dp.Files;
using System.Text.Json.Serialization;

namespace Lib3Dp.State
{
	public interface IMachinePrintJob
	{
		string Name { get; }
		string? CustomID { get; }
		int PercentageComplete { get; }
		TimeSpan RemainingTime { get; }
		TimeSpan TotalTime { get; }
		MachineMessage? Issue { get; }
		MachineFileHandle? File { get; }
		MachineFileHandle? Thumbnail { get; }
		string? SubStage { get; }
		public int? TotalMaterialUsage { get; set; }
		int? PrintSpeedPercent { get; }

		/// <summary>
		/// Maps each material source used by this print job to the amount consumed in grams.
		/// Excluded from JSON — use PrintJob.SpoolMaterialUsages via a Full subscription instead.
		/// </summary>
		[JsonIgnore]
		IReadOnlyDictionary<SpoolLocation, int>? MaterialUsages { get; }
	}
}
