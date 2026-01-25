using Connect3Dp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Connect3Dp.Connectors.Creality
{
    public class CrealityK1Connector : MachineConnector
    {
        private readonly Logger Logger;
        private readonly SimpleWebSocketClient Websocket;

        public CrealityK1Connector(Uri address, string nickname, string uid) : base(nickname, uid, "Creality", "K1C")
        {
            Websocket = new(address);

            this.Logger = Logger.OfCategory($"CrealityK1Connector ({uid})");

            this.Websocket.OnMessage += Websocket_OnMessage;
        }

        public override object GetConfiguration()
        {
            throw new NotImplementedException();
        }

        private void Websocket_OnMessage(string msg)
        {
            Logger.Trace(msg);
        }

        protected override async Task Connect_Internal(CancellationToken cancellationToken = default)
        {
            await this.Websocket.ConnectAsync(cancellationToken);

        }



    }
}
