
using Lib3Dp.Connectors;
using Lib3Dp.State;

namespace Connect3Dp
{
	public record struct OnMachineRemovedArgs(string ID);
	public record struct OnMachineAddedArgs(string ID);
	public record struct OnMachineUpdatedArgs(string ID, object UpdatedConfig);

	/// <summary>
	/// An interface to retrieve, update, and remove certain machine connections.
	/// </summary>
	public interface IMachineConfigurationService
	{
		Task<IEnumerable<MachineConnection>> LoadMachines();

		Task SaveMachine(string ID, object configuration);

		Task DeleteMachine(string ID);

		event EventHandler<OnMachineAddedArgs>? OnAdded;
		event EventHandler<OnMachineRemovedArgs>? OnRemoved;
		event EventHandler<OnMachineUpdatedArgs>? OnUpdated;
	}

	public class FileBasedMachineCollectionService : IHostedService, IHostedLifecycleService, IMachineConfigurationService
	{
		private readonly ILogger<FileBasedMachineCollectionService> Logger;

		public event EventHandler<OnMachineAddedArgs>? OnAdded;
		public event EventHandler<OnMachineRemovedArgs>? OnRemoved;
		public event EventHandler<OnMachineUpdatedArgs>? OnUpdated;

		public FileBasedMachineCollectionService(ILogger<FileBasedMachineCollectionService> logger)
		{
			this.Logger = logger;
		}

		public Task DeleteMachine(string ID)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<MachineConnection>> LoadMachines()
		{
			throw new NotImplementedException();
		}

		public Task SaveMachine(string ID, object configuration)
		{
			throw new NotImplementedException();
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();

			// 1. Read Machine Collection
		}

		public Task StartedAsync(CancellationToken cancellationToken)
		{
			//throw new NotImplementedException();
			return Task.CompletedTask;
		}

		public Task StartingAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task StoppedAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task StoppingAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
