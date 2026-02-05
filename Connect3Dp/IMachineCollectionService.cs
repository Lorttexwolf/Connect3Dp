using Lib3Dp.Connectors;
using Lib3Dp.State;

namespace Connect3Dp
{
	public record struct MachineChangesEventArgs(string ID, IReadOnlyMachineState UpdatedState);

	public interface IMachineCollectionService
	{
		event EventHandler<MachineChangesEventArgs>? OnChanges;
	}

	public class MachineCollectionService : IMachineCollectionService, IHostedService
	{
		private readonly IMachineConfigurationService Configurations;
		private readonly HashSet<MachineConnection> Connections;

		public event EventHandler<MachineChangesEventArgs>? OnChanges;

		public MachineCollectionService(IMachineConfigurationService configurationService)
		{
			this.Configurations = configurationService;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			// Load Connections from Configurations.

			var loadedMachines = await Configurations.LoadMachines();

			foreach (var loadedMachine in loadedMachines)
			{
				loadedMachine.OnChange += LoadedMachine_OnChange;

				Connections.Add(loadedMachine);
			}

			// Connect to all Machines

			await Parallel.ForEachAsync(Connections, new ParallelOptions()
			{
				MaxDegreeOfParallelism = 10,

			}, (bleh, _) => new ValueTask(bleh.Connect(_)));
		}

		private void LoadedMachine_OnChange(object? sender, IReadOnlyMachineState e)
		{
			this.OnChanges?.Invoke(this, new MachineChangesEventArgs(e.ID, e));
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}
}
