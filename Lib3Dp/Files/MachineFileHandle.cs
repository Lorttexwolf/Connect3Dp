using System.Text.Json.Serialization;

namespace Lib3Dp.Files
{
	public record struct MachineFileHandle(
		string MachineID,
		[property: JsonPropertyName("uri")] string URI,
		[property: JsonPropertyName("mime")] string MIME,
		[property: JsonPropertyName("hashSha256")] string HashSHA256)
	{
		public override readonly string ToString()
		{
			return $"{MIME} {URI}";
		}
	}
}
