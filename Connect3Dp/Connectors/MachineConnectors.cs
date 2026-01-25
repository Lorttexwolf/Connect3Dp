using Connect3Dp.Extensions;
using Connect3Dp.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Connect3Dp.Connectors
{
    public static class MachineConnectors
    {
        private static readonly Logger Logger = Logger.OfCategory(nameof(MachineConnectors));

        private static readonly Dictionary<Type, Type> ConnectorTypeToConfigTypes = [];
        private static readonly Dictionary<string, Type> ConnectorFullNameToConnectorTypes = [];

        static MachineConnectors()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var connectorTypes = assembly.GetTypes().Where(t => typeof(IConfigurableConnector).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var connectorType in connectorTypes)
            {
                var getConfigMethod = connectorType.GetMethod("GetConfigurationType", BindingFlags.Static | BindingFlags.Public)!;

                var configType = (Type)getConfigMethod.Invoke(null, null)!;

                ConnectorTypeToConfigTypes[connectorType] = configType;
                ConnectorFullNameToConnectorTypes[connectorType.FullName!] = connectorType;

                Logger.Trace($"Discovered configurable {connectorType.FullName}");
            }
        }

        public static bool TryParseConfigurationAsJSON(FileStream stream, [NotNullWhen(true)] out MachineConnector? connector)
        {
            connector = null;

            var streamAsJSON = JsonDocument.Parse(stream);

            if (!streamAsJSON.RootElement.TryGetString(out var connectorTypeFullName, "ConnectorTypeFullName")
                || !ConnectorFullNameToConnectorTypes.TryGetValue(connectorTypeFullName, out var connectorType)
                || !ConnectorTypeToConfigTypes.TryGetValue(connectorType, out var configType))
            {
                return false;
            }

            var config = JsonSerializer.Deserialize(streamAsJSON, configType)!;

            var makeWithConfigMethod = connectorType.GetMethod(nameof(IConfigurableConnector.CreateFromConfiguration), BindingFlags.Static | BindingFlags.Public)!;

            connector = makeWithConfigMethod.Invoke(null, [config]) as MachineConnector;

            return connector != null;
        }

        //public static bool TryParseConnectorAsJSON<T>(FileStream stream, [NotNullWhen(true)] out T? connector) where T : MachineConnector, IConfigurableConnector
        //{
        //    connector = null;

        //    try
        //    {
        //        var configType = T.GetConfigurationType();
        //        var config = JsonSerializer.Deserialize(stream, configType)!;

        //        connector = (T)T.CreateFromConfiguration(config);

        //        return connector != null;
        //    }
        //    catch (Exception)
        //    {
        //        return false;
        //    }
        //}
    }
}
