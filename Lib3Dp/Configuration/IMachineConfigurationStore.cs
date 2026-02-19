
namespace Lib3Dp.Configuration
{
	public interface IMachineConfigurationStore
	{
		public Task StoreConfiguration(string machineID, ConfigurationWithDiscrimination configurationWithDiscrimination);

		public Task<ConfigurationWithDiscrimination[]> ReadConfigurations();
	}
}
