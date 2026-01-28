using Cronos;

namespace Connect3Dp.State
{
    public record HeatingSchedule(CronExpression Timing, HeatingSettings Settings)
    {
        internal Guid? SchedulerID { get; set; }
    }
}
