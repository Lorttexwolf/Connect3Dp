namespace Connect3Dp.State
{
    public interface IReadOnlyMachinePrintJob
    {
        string Name { get; }
        MachineFile? File { get; }
        MachineFile? Thumbnail { get; }
        string? Stage { get; }
        int PercentageComplete { get; }
        TimeSpan RemainingTime { get; }
        TimeSpan TotalTime { get; }
    }
}
