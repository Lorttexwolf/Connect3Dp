namespace Lib3Dp.Files
{
	public record struct MachineFileHandle(string MachineID, string URI, string MIME, string HashSHA256)
	{
		public override readonly string ToString()
		{
			return $"{MIME} {URI}";
		}
	}
}
