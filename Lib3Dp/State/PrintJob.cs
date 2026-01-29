using PartialBuilderSourceGen;

namespace Lib3Dp.State
{
	[GeneratePartialBuilder]
	internal partial class PrintJob : IReadOnlyMachinePrintJob, IEquatable<PrintJob?>, ICloneable
	{
		public required string Name { get; set; }
		public required int PercentageComplete { get; set; }
		public required TimeSpan RemainingTime { get; set; }
		public required TimeSpan TotalTime { get; set; }
		public MachineFile? Thumbnail { get; set; }
		public MachineFile? File { get; set; }
		public string? Stage { get; set; }

		public object Clone()
		{
			return new PrintJob()
			{
				Name = Name,
				Thumbnail = Thumbnail,
				File = File,
				Stage = Stage,
				PercentageComplete = PercentageComplete,
				RemainingTime = RemainingTime,
				TotalTime = TotalTime,
			};
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as PrintJob);
		}

		public bool Equals(PrintJob? other)
		{
			return other is not null &&
				   Name == other.Name &&
				   EqualityComparer<MachineFile?>.Default.Equals(Thumbnail, other.Thumbnail) &&
				   EqualityComparer<MachineFile?>.Default.Equals(File, other.File) &&
				   Stage == other.Stage &&
				   PercentageComplete == other.PercentageComplete &&
				   RemainingTime.Equals(other.RemainingTime) &&
				   TotalTime.Equals(other.TotalTime);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Name, Thumbnail, File, Stage, PercentageComplete, RemainingTime, TotalTime);
		}

		public static bool operator ==(PrintJob? left, PrintJob? right)
		{
			return EqualityComparer<PrintJob>.Default.Equals(left, right);
		}

		public static bool operator !=(PrintJob? left, PrintJob? right)
		{
			return !(left == right);
		}
	}
}
