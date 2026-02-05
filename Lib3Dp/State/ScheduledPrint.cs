using Cronos;

namespace Lib3Dp.State
{
	public record ScheduledPrint(CronExpression Timing, LocalPrintJob LocalJob, PrintOptions Options)
	{
		/// <summary>
		/// Internal scheduler task ID used to track and cancel the scheduled task.
		/// </summary>
		internal Guid? SchedulerID { get; set; }
	}
}
