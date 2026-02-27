
using Lib3Dp.Files;

namespace Lib3Dp.Configuration
{
	public record struct MachineIDWithConfigurationWithDiscrimination(string MachineID, ConfigurationWithDiscrimination ConfigurationWithDiscrimination);

	/// <summary>
	/// An interface to retrieve, store, and delete certain machine configurations from <see cref="IConfigurableConnection.GetConfigurationWithDiscrimination"/>.
	/// </summary>
	public interface IMachineConfigurationStore
	{
		Task<MachineIDWithConfigurationWithDiscrimination[]> LoadConfigurations();

		Task StoreConfiguration(string ID, ConfigurationWithDiscrimination configuration);

		Task RemoveConfiguration(string ID);
	}
}
