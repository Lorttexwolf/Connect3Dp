using Connect3Dp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Connect3Dp.Connectors.ELEGOO
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

    public class ELEGOOMachineConnector : MachineConnector
    {
        private readonly SimpleWebSocketClient Socket;
        private readonly ELEGOOMachineConfiguration _Configuration;

        public ELEGOOMachineConnector(ELEGOOMachineConfiguration configuration) : base(configuration.Nickname, configuration.Nickname, "ELEGOO", configuration.Model.ToString())
        {
            this._Configuration = configuration;
            this.Socket = new SimpleWebSocketClient(_Configuration.IPAddress);

            this.Socket.OnMessage += (message) =>
            {
                Console.WriteLine($"Received Message {message}");
            };
        }

        public override object GetConfiguration()
        {
            throw new NotImplementedException();
        }

        protected override async Task Connect_Internal(CancellationToken cancellationToken = default)
        {
            await this.Socket.ConnectAsync(cancellationToken);

            // TODO, do Discovery, configure Model, and etc from Discovery payload.

            CommitState(changes => changes.SetIsConnected(true));
        }
    }
}
