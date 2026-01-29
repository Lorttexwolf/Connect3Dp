using Lib3Dp.Connectors;

namespace Lib3Dp
{
	public interface IConfigurableConnector
	{
		object GetConfiguration();

		public static abstract Type GetConfigurationType();

		public static abstract MachineConnection CreateFromConfiguration(object configuration);
	}

	public interface IConnectorConfiguration
	{
		string ConnectorTypeFullName { get; }
	}
}
