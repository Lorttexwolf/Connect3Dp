using Connect3Dp.Connectors;
using System.Text.Json.Serialization;

namespace Connect3Dp
{
    public interface IConfigurableConnector
    {
        object GetConfiguration();

        public static abstract Type GetConfigurationType();

        public static abstract MachineConnector CreateFromConfiguration(object configuration);
    }

    public interface IConnectorConfiguration
    {
        string ConnectorTypeFullName { get; }
    }
}
