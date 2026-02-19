using Lib3Dp.Configuration;
using Lib3Dp.Files;
using Lib3Dp.Utilities;

namespace Lib3Dp.Connectors.Creality
{
	public record CrealityK1CConfiguration(
		string? Nickname,
		string Address,
		string SerialNumber
	);

	public class CrealityK1CConnection : MachineConnection, IConfigurableConnection<CrealityK1CConnection, CrealityK1CConfiguration>
	{
		private readonly Logger Logger;
		private readonly SimpleWebSocketClient WebSocket;

		private CrealityK1CConfiguration Configuration;

		public CrealityK1CConnection(IMachineFileStore fileStore, CrealityK1CConfiguration configuration) : base(
			fileStore, 
			configuration: new MachineConnectionConfiguration(
				Nickname: configuration.Nickname, 
				ID: configuration.SerialNumber, 
				Brand: "Creality", 
				Model: "K1C"))
		{
			Configuration = configuration;
			WebSocket = new(new Uri($"ws://{configuration.Address}/ws")); // TODO: Confirm if it's /ws and check port.

			this.Logger = Logger.OfCategory($"{nameof(CrealityK1CConnection)} ({configuration.SerialNumber ?? configuration.Nickname})");

			this.WebSocket.OnMessage += Websocket_OnMessage;
		}

		#region Configuration

		CrealityK1CConfiguration IConfigurableConnection<CrealityK1CConnection, CrealityK1CConfiguration>.GetConfiguration()
		{
			return Configuration;
		}

		public async Task<MachineOperationResult> UpdateConfiguration(CrealityK1CConfiguration updatedCfg)
		{
			this.Configuration = updatedCfg;

			// TODO: Use updatedCfg to update web socket address and etc.

			return MachineOperationResult.Ok;
		}

		public static Type GetConfigurationType() => typeof(CrealityK1CConfiguration);

		public static CrealityK1CConnection CreateFromConfiguration(IMachineFileStore fileStore, CrealityK1CConfiguration configuration)
		{
			return new CrealityK1CConnection(fileStore, configuration);
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

		private void Websocket_OnMessage(string msg)
		{
			Logger.Trace(msg);
		}

		protected override async Task Connect_Internal()
		{
			await this.WebSocket.ConnectAsync();

		}
	}
}
