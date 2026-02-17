using Lib3Dp.Files;
using Lib3Dp.State;
using Lib3Dp.Utilities;

namespace Lib3Dp.Connectors.ELEGOO
{
	public enum ELEGOOMachineKind
	{
		CentauriCarbon
	}

	public struct ELEGOOMachineConfiguration
	{
		public string Nickname;
		public ELEGOOMachineKind Model;
		public Uri IPAddress;
	}

	public class ELEGOOMachineConnector : MachineConnection
	{
		private readonly SimpleWebSocketClient Socket;
		private readonly ELEGOOMachineConfiguration Configuration;

		public ELEGOOMachineConnector(IMachineFileStore fileStore, ELEGOOMachineConfiguration configuration) : base(fileStore, configuration.Nickname, configuration.Nickname, "ELEGOO", configuration.Model.ToString())
		{
			this.Configuration = configuration;
			this.Socket = new SimpleWebSocketClient(Configuration.IPAddress);

			this.Socket.OnMessage += (message) =>
			{
				Console.WriteLine($"Received Message {message}");
			};
		}

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

			CommitState(changes => changes.SetIsConnected(true));
		}

	}
}
