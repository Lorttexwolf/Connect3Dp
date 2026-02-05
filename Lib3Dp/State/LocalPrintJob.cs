namespace Lib3Dp.State
{
	public record MaterialToPrint(Material Material, int TotalGramsUsed, double NozzleDiameter);

	public record LocalPrintJob(string Name, string Path, int TotalGramsUsed, TimeSpan Time, Dictionary<int, MaterialToPrint> MaterialsToPrint);
}
