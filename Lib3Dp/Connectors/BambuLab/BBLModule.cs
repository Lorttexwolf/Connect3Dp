using System.Text.Json.Serialization;

namespace Lib3Dp.Connectors.BambuLab.MQTT
{
	internal partial class BBLMQTTConnection
	{
		internal struct BBLModule
		{
			[JsonPropertyName("name")]
			public string InternalName { get; set; }

			[JsonPropertyName("product_name")]
			public string ProductName { get; set; }

			[JsonPropertyName("sw_ver")]
			public BBLFirmwareVersion Version { get; set; }

			[JsonPropertyName("sn")]
			public string SN { get; set; }
		}
	}
}
