using Lib3Dp.Files;
using Lib3Dp.State;
using Lib3Dp.Utilities;

namespace Lib3Dp.Connectors.Creality
{
	public class CrealityK1Connector : MachineConnection
	{
		private readonly Logger Logger;
		private readonly SimpleWebSocketClient Websocket;

		public CrealityK1Connector(IMachineFileStore fileStore, Uri address, string nickname, string uid) : base(fileStore, nickname, uid, "Creality", "K1C")
		{
			Websocket = new(address);

			this.Logger = Logger.OfCategory($"CrealityK1Connector ({uid})");

			this.Websocket.OnMessage += Websocket_OnMessage;
		}

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
			await this.Websocket.ConnectAsync();

		}
	}
}
