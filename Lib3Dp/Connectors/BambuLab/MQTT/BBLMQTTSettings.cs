using Lib3Dp.Connectors.BambuLab.Constants;

namespace Lib3Dp.Connectors.BambuLab.MQTT
{
	public record struct BBLMQTTSettings(string Address, string SerialNumber, string AccessCode, string Model);
}
