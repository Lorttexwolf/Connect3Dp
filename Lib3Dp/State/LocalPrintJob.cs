namespace Lib3Dp.State
{
	public record struct LocalPrintJob(string Name, MachineFileHandle File, int TotalGramsUsed, TimeSpan Time, Dictionary<int, MaterialToPrint> MaterialsToPrint);
}
