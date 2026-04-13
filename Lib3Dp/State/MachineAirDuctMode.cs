using System.Text.Json.Serialization;

namespace Lib3Dp.State
{
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public enum MachineAirDuctMode
	{
		None = 0,
		/// <summary>
		/// For example, the Bambu Lab P2S air conditioning in cooling mode.
		/// </summary>
		Cooling = 1,
		/// <summary>
		/// For example, the Bambu Lab P2S air conditioning in heating mode.
		/// </summary>
		Heating = 2
	}
}
