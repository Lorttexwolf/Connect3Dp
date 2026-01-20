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
    //public enum ELEGOOMachineKind
    //{
    //    Centauri
    //}

    //public struct ELEGOOMachineConfiguration
    //{
    //    public string Nickname;
    //    public ELEGOOMachineKind Model;
    //    public Uri IPAddress;
    //}

    //internal class ELEGOOMachineConnector : MachineConnector
    //{
    //    private readonly JEWebSocket Socket;
    //    private readonly ELEGOOMachineConfiguration _Configuration;

    //    public override object Configuration => _Configuration;

    //    public ELEGOOMachineConnector(ELEGOOMachineConfiguration configuration) : base(configuration.Nickname, configuration.Nickname, "ELEGOO", configuration.Model.ToString())
    //    {
    //        this._Configuration = configuration;
    //        this.Socket = new JEWebSocket(_Configuration.IPAddress);

    //        this.Socket.OnMessage += (message) =>
    //        {
    //            Console.WriteLine($"Received Message {message}");
    //        };
    //    }

    //    protected override async Task<bool> Connect_Internal()
    //    {
    //        await this.Socket.Connect(CancellationToken.None);

    //        return true;
    //    }
    //}
}
