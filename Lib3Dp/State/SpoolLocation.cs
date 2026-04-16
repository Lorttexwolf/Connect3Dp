using System.Text.Json.Serialization;

namespace Lib3Dp.State
{
	public readonly record struct SpoolLocation(
		[property: JsonPropertyName("muid")] string MUID,
		int Slot)
	{
		/// <summary>
		/// Returns a compact string used as the JSON dictionary key when SpoolLocation is a dict key,
		/// e.g. in PrintJob.SpoolMaterialUsages. Format: "{MUID}:{Slot}".
		/// </summary>
		public override string ToString()
		{
			return $"{MUID}:{Slot}";
		}
	}
}
