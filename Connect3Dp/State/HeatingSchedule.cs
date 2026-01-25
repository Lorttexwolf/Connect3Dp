using Cronos;

namespace Connect3Dp.State
{
    public interface IHeatingSchedule
    {
        CronExpression Timing { get; }
        HeatingSettings Settings { get; } 
        int? OnlyAboveHumidityPercent { get; }
    }

    public record HeatingSchedule : IHeatingSchedule
    {
        public required CronExpression Timing { get; init; }
        public required HeatingSettings Settings { get; init; }
        public int? OnlyAboveHumidityPercent { get; init; }

        internal Guid? SchedulerID { get; set; }
    }
}
