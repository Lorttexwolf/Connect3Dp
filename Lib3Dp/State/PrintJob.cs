using MQTTnet;
using PartialBuilderSourceGen.Attributes;

namespace Lib3Dp.State
{
	[GeneratePartialBuilder]
	public record PrintJob : IMachinePrintJob
	{
		public required string Name { get; set; }
		public required int PercentageComplete { get; set; }
		public required TimeSpan RemainingTime { get; set; }
		public required TimeSpan TotalTime { get; set; }
		public MachineMessage? Issue { get; set; }
		public MachineFileHandle? Thumbnail { get; set; }
		public MachineFileHandle? File { get; set; }
		public string? SubStage { get; set; }
		public int? TotalMaterialUsage { get; set; }
		public string? LocalPath { get; set; }
		public Dictionary<SpoolLocation, int>? SpoolMaterialUsages { get; set; }

		IReadOnlyDictionary<SpoolLocation, int>? IMachinePrintJob.MaterialUsages => SpoolMaterialUsages;
	}
}
