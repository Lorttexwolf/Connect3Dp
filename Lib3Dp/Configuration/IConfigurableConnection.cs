using Lib3Dp.Connectors;
using Lib3Dp.Files;
using System;
using System.Runtime.CompilerServices;

namespace Lib3Dp.Configuration
{
	public interface IConfigurableConnection
	{
		object GetConfiguration();

		Task<MachineOperationResult> UpdateConfiguration(object updatedCfg);

		static abstract Type GetConfigurationType();

		static abstract MachineConnection CreateFromConfiguration(IMachineFileStore fileStore, object configuration);

		ConfigurationWithDiscrimination GetConfigurationWithDiscrimination()
		{
			object cfg = GetConfiguration()!;
			string cfgTypeFullName = cfg.GetType().FullName ?? throw new Exception("Configuration type must have a name");

			return new ConfigurationWithDiscrimination(cfg!, cfgTypeFullName);
		}
	}

	public interface IConfigurableConnection<C, T> : IConfigurableConnection 
		where T : notnull 
		where C : MachineConnection, IConfigurableConnection<C, T>
	{
		new T GetConfiguration();

		object IConfigurableConnection.GetConfiguration()
		{
			return GetConfiguration();
		}

		static Type IConfigurableConnection.GetConfigurationType()
		{
			return typeof(T);
		}

		Task<MachineOperationResult> UpdateConfiguration(T updatedCfg);

		Task<MachineOperationResult> IConfigurableConnection.UpdateConfiguration(object updatedCfg)
		{
			if (updatedCfg is T tUpdatedCfg)
			{
				return UpdateConfiguration(tUpdatedCfg);
			}
			throw new ArgumentException($"Configuration must be of type {typeof(T).FullName}", nameof(updatedCfg));
		}

		static abstract C CreateFromConfiguration(IMachineFileStore fileStore, T configuration);

		static MachineConnection IConfigurableConnection.CreateFromConfiguration(IMachineFileStore fileStore, object configuration)
		{
			if (configuration is T tConfiguration)
			{
				return C.CreateFromConfiguration(fileStore, tConfiguration);
			}
			throw new ArgumentException($"Configuration must be of type {typeof(T).FullName}", nameof(configuration));
		}
	}
}
