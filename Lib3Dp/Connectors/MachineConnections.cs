using Lib3Dp.Extensions;
using Lib3Dp.Utilities;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;

namespace Lib3Dp.Connectors
{
	public static class MachineConnections
	{
		private static readonly Logger Logger = Logger.OfCategory(nameof(MachineConnections));

		private static readonly Dictionary<Type, Type> ConnectorTypeToConfigTypes = [];
		private static readonly Dictionary<string, Type> ConnectorFullNameToConnectorTypes = [];

		static MachineConnections()
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

		public static bool TryParseConfigurationAsJSON(FileStream stream, [NotNullWhen(true)] out MachineConnection? connector)
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

			connector = makeWithConfigMethod.Invoke(null, [config]) as MachineConnection;

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
