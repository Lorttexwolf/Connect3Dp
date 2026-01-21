using Connect3Dp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connect3Dp.Connectors.Creality
{
    public class CrealityK1Connector(Uri address, string nickname, string uid) : MachineConnector(nickname, uid, "Creality", "K1C")
    {
        private readonly SimpleWebSocketClient Websocket = new(address);

        protected override async Task Connect_Internal(CancellationToken cancellationToken = default)
        {
            await this.Websocket.ConnectAsync(cancellationToken);
        }
    }
}
