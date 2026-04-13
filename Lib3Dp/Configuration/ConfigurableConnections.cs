using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Lib3Dp.Connectors;
using Lib3Dp.Files;

namespace Lib3Dp.Configuration
{
	public static class ConfigurableConnections
	{
		private static readonly Dictionary<string, Type> ConfigTypeByDiscrimination = [];
		private static readonly Dictionary<string, Func<IMachineFileStore, object, MachineConnection>> Creators = [];

		static ConfigurableConnections()
		{
			var assemblyToSearch = Assembly.GetExecutingAssembly();

			var connectorTypes = assemblyToSearch.GetTypes()
				.Where(t => typeof(IConfigurableConnection).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

			foreach (var connectorType in connectorTypes)
			{
				var getConfigMethod = connectorType.GetMethod("GetConfigurationType", BindingFlags.Static | BindingFlags.Public);
				if (getConfigMethod == null) continue;

				var cfgType = getConfigMethod.Invoke(null, null) as Type;
				if (cfgType == null) continue;

				var discrimination = cfgType.FullName;
				if (discrimination == null) continue;

				var createMethod = connectorType.GetMethod("CreateFromConfiguration", BindingFlags.Static | BindingFlags.Public);
				if (createMethod == null) continue;

				ConfigTypeByDiscrimination[discrimination] = cfgType;
				Creators[discrimination] = (fileStore, configuration) =>
				{
					var created = createMethod.Invoke(null, [fileStore, configuration]);
					return created as MachineConnection ?? throw new InvalidOperationException($"CreateFromConfiguration on {connectorType.FullName} did not return a MachineConnection");
				};
			}
		}

		public static IEnumerable<string> KnownDiscriminations() => ConfigTypeByDiscrimination.Keys;

		public static bool TryGetConfigurationType(string discrimination, out Type? configurationType)
		{
			return ConfigTypeByDiscrimination.TryGetValue(discrimination, out configurationType);
		}

		public static bool TryCreateFromConfiguration(MachineIDWithConfigurationWithDiscrimination cfg, IMachineFileStore fileStore, [NotNullWhen(true)] out MachineConnection? connection)
		{
			connection = null;
			if (cfg.ConfigurationWithDiscrimination.Discrimination == null) return false;

			if (!Creators.TryGetValue(cfg.ConfigurationWithDiscrimination.Discrimination, out var creator)) return false;

			try
			{
				connection = creator(fileStore, cfg.ConfigurationWithDiscrimination.Configuration);
				return connection != null;
			}
			catch
			{
				connection = null;
				return false;
			}
		}
	}
}
