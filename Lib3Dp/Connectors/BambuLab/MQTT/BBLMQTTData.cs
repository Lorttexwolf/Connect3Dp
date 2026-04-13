using Lib3Dp.State;

namespace Lib3Dp.Connectors.BambuLab.MQTT
{
	internal record struct BBLMQTTData(MachineStateUpdate Changes, BBLFirmwareVersion? FirmwareVersion, bool? UsesUnsupportedSecurity, bool? UpdateAMSMapping, bool? HasUSBOrSDCard);
}
