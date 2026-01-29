namespace Lib3Dp.State
{
	public record HistoricPrintJob(string Name, bool IsSuccess, DateTime EndedAt, TimeSpan Elapsed, MachineFile? Thumbnail, MachineFile? File);
}
