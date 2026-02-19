using Lib3Dp.Configuration;
using Lib3Dp.Files;
using Lib3Dp.Utilities;
using Lib3Dp.State;

namespace Lib3Dp.Connectors.ELEGOO
{


	public enum ELEGOOMachineKind
	{
		CentauriCarbon,
		CentauriCarbon2,
	}

	public record ELEGOOMachineConfiguration(
		string? Nickname,
		ELEGOOMachineKind Model,
		string SerialNumber,
		string IPAddress
	);

	public class ELEGOOMachineConnector : MachineConnection, IConfigurableConnection<ELEGOOMachineConnector, ELEGOOMachineConfiguration>
	{
		private readonly SimpleWebSocketClient Socket;
		
		public ELEGOOMachineConfiguration Configuration { get; private set; }

		public ELEGOOMachineConnector(IMachineFileStore fileStore, ELEGOOMachineConfiguration config) 
			: base(fileStore, new MachineConnectionConfiguration(config.Nickname, $"ELEGOO{config.SerialNumber}", "ELEGOO", "Centauri Carbon"))
		{
			this.Configuration = config;

			this.Socket = new SimpleWebSocketClient(new Uri(Configuration.IPAddress));

			this.Socket.OnMessage += (message) =>
			{
				Console.WriteLine($"Received Message {message}");
			};
		}

		#region Configuration

		ELEGOOMachineConfiguration IConfigurableConnection<ELEGOOMachineConnector, ELEGOOMachineConfiguration>.GetConfiguration()
		{
			throw new NotImplementedException();
		}

		public Task<MachineOperationResult> UpdateConfiguration(ELEGOOMachineConfiguration updatedCfg)
		{
			throw new NotImplementedException();
		}

		public static Type GetConfigurationType()
		{
			throw new NotImplementedException();
		}

		public static ELEGOOMachineConnector CreateFromConfiguration(IMachineFileStore fileStore, ELEGOOMachineConfiguration configuration)
		{
			throw new NotImplementedException();
		}

		#endregion

		protected override Task DownloadLocalFile(MachineFileHandle fileHandle, Stream destinationStream)
		{
			throw new NotImplementedException();
		}

		public override object GetConfiguration()
		{	
			throw new NotImplementedException();
		}

		protected override async Task Connect_Internal()
		{
			await this.Socket.ConnectAsync();

			// TODO, do Discovery, configure Model, and etc from Discovery payload.

			CommitState(changes => changes.SetStatus(MachineStatus.Disconnected));
		}
	}
}
