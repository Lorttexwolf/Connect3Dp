using Cronos;

namespace Lib3Dp.State
{
	public record HeatingSchedule(CronExpression Timing, HeatingSettings Settings)
	{
		internal Guid? SchedulerID { get; set; }
	}
}
