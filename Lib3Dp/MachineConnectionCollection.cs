using Lib3Dp.Configuration;
using Lib3Dp.Connectors;
using Lib3Dp.Files;
using Lib3Dp.State;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib3Dp
{
	public record struct OnMachineRemovedArgs(string MachineID);
	public record struct OnMachineAddedArgs(string MachineID, ConfigurationWithDiscrimination Config);
	public record struct OnMachineConfigurationUpdatedArgs(string MachineID, ConfigurationWithDiscrimination UpdatedConfig);
	public record struct OnMachineStateUpdatedArgs(string MachineID, MachineStateChanges Changes);

	public class MachineConnectionCollection
	{
		private readonly ILogger<MachineConnectionCollection> Logger;

		public IMachineConfigurationStore ConfigurationStore { get; }
		public IMachineFileStore FileStore { get; }

		private ConcurrentDictionary<string, MachineConnection> _Connections { get; }
		public IReadOnlyDictionary<string, MachineConnection> Connections => _Connections;

		public event EventHandler<OnMachineStateUpdatedArgs>? OnStateChange;
		public event EventHandler<OnMachineAddedArgs>? OnMachineAdded;
		public event EventHandler<OnMachineRemovedArgs>? OnMachineRemoved;
		public event EventHandler<OnMachineConfigurationUpdatedArgs>? OnMachineConfigurationUpdated;

		public MachineConnectionCollection(IMachineConfigurationStore configurationStore, IMachineFileStore fileStore, ILogger<MachineConnectionCollection> logger)
		{
			this.Logger = logger;

			this.ConfigurationStore = configurationStore;
			this.FileStore = fileStore;

			this._Connections = [];
		}

		/// <summary>
		/// Attempts to connect to all <see cref="MachineConnection"/> inside <see cref="Connections"/> if <see cref="MachineStatus.Disconnected"/>.
		/// </summary>
		public Task ConnectIfDisconnected(CancellationToken cancellationToken)
		{
			return Parallel.ForEachAsync(_Connections.Values, cancellationToken, async (connection, ct) =>
			{
				await connection.ConnectIfDisconnected(ct);
			});
		}

		/// <summary>
		/// Populates <see cref="Connections"/> from the <see cref="ConfigurationStore"/> instance.
		/// </summary>
		/// <remarks>
		/// If the connection exists and configuration has been changed, the existing <see cref="MachineConnection"/> configuration will be updated.
		/// </remarks>
		public async Task LoadFromConfigurationStore()
		{
			var loadedConfigurations = await this.ConfigurationStore.LoadConfigurations();

			// Create Machine Connections from Machine Configurations.
			await LoadFromConfigurations(loadedConfigurations);
		}

		public async Task LoadFromConfigurations(MachineIDWithConfigurationWithDiscrimination[] cfgs)
		{
			foreach (var cfg in cfgs)
			{
				if (!ConfigurableConnections.TryCreateFromConfiguration(cfg, this.FileStore, out var createdConnection))
				{
					Logger.LogError("Unable to load Machine with Configuration: {}", cfg);
					continue;
				}

				if (_Connections.TryGetValue(cfg.MachineID, out var existingConnection))
				{
					if (existingConnection is IConfigurableConnection c)
					{
						if (existingConnection.GetConfiguration().Equals(cfg.ConfigurationWithDiscrimination.Configuration)) continue;

						try
						{
							var confOp = await c.UpdateConfiguration(cfg.ConfigurationWithDiscrimination.Configuration);

							confOp.ThrowIfFailed();
						}
						catch (Exception ex)
						{
							Logger.LogError(ex, "Failed to Update Configuration of Machine {}", cfg.MachineID);
							continue;
						}
					}
					else
					{
						await existingConnection.Disconnect();

						// Once disconnected, replace it with the newly created configuration.

						_Connections[cfg.MachineID] = createdConnection;

						ListenForChanges(createdConnection);
					}

					this.OnMachineConfigurationUpdated?.Invoke(this, new OnMachineConfigurationUpdatedArgs(cfg.MachineID, cfg.ConfigurationWithDiscrimination));
				}
				else
				{
					_Connections[cfg.MachineID] = createdConnection;

					ListenForChanges(createdConnection);

					this.OnMachineAdded?.Invoke(this, new OnMachineAddedArgs(cfg.MachineID, cfg.ConfigurationWithDiscrimination));
				}
			}

		}

		public async Task<bool> Remove(string machineID)
		{
			if (!this._Connections.TryRemove(machineID, out var removedConnection))
			{
				return false;
			}

			await removedConnection.Disconnect();

			removedConnection.OnChanges -= MachineConnection_OnChanges;

			OnMachineRemoved?.Invoke(this, new OnMachineRemovedArgs(machineID));

			return true;
		}

		public async Task Add(MachineConnection connection, bool connectIfDisconnected = true)
		{
			if (this._Connections.TryGetValue(connection.ID, out var existingConnection))
			{
				throw new NotImplementedException();
			}
			else
			{
				this._Connections[connection.ID] = connection;

				ListenForChanges(connection);
				if (connectIfDisconnected)
				{
					_ = connection.ConnectIfDisconnected();
				}

				if (connection is IConfigurableConnection c)
				{
					await this.ConfigurationStore.StoreConfiguration(connection.ID, c.GetConfigurationWithDiscrimination());
				}
			}
		}

		protected virtual void ListenForChanges(MachineConnection machineConnection)
		{
			machineConnection.OnChanges += MachineConnection_OnChanges;
		}

		private void MachineConnection_OnChanges(MachineConnection connection, MachineStateChanges changes)
		{
			OnStateChange?.Invoke(this, new OnMachineStateUpdatedArgs(connection.ID, changes));
		}
	}
}
