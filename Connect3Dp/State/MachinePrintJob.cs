using PartialSourceGen;

namespace Connect3Dp.State
{
    internal partial class PrintJob : IReadOnlyMachinePrintJob
    {
        public required string Name { get; set; }
        public MachineFile? Thumbnail { get; set; }
        public MachineFile? File { get; set; }
        public string? Stage { get; set; }
        public required int PercentageComplete { get; set; }
        public required TimeSpan RemainingTime { get; set; }
        public required TimeSpan TotalTime { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is PrintJob job &&
                   Name == job.Name &&
                   Thumbnail == job.Thumbnail &&
                   Stage == job.Stage &&
                   PercentageComplete == job.PercentageComplete &&
                   RemainingTime.Equals(job.RemainingTime) &&
                   TotalTime.Equals(job.TotalTime);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Thumbnail, Stage, PercentageComplete, RemainingTime, TotalTime);
        }
    }
}
