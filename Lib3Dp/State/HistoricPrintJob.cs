namespace Lib3Dp.State
{
	public readonly record struct HistoricPrintJob(string Name, bool IsSuccess, DateTime EndedAt, TimeSpan Elapsed, MachineFileHandle? Thumbnail, MachineFileHandle? File);
}
