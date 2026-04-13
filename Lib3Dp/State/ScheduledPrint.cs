using Cronos;
using System.Text.Json.Serialization;

namespace Lib3Dp.State
{
	public record ScheduledPrint(CronExpression Timing, LocalPrintJob LocalJob, PrintOptions Options, [property: JsonIgnore] Dictionary<SpoolLocation, Material> InitialMapping)
	{
		/// <summary>
		/// Internal scheduler task ID used to track and cancel the scheduled task.
		/// </summary>
		internal Guid? SchedulerID { get; set; }
	}
}
