using Lib3Dp.Connectors;
using Lib3Dp.Files;

namespace Lib3Dp
{
	public interface IConfigurableConnector
	{
		object GetConfiguration();

		public static abstract Type GetConfigurationType();

		public static abstract MachineConnection CreateFromConfiguration(IMachineFileStore fileStore, object configuration);
	}

	public interface IConnectorConfiguration
	{
		string ConnectorTypeFullName { get; }
	}

	public interface IConfigurable
	{
		object GetConfiguration();
	}
}
