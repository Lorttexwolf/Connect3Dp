using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Connect3Dp.Utilities
{
    internal class JEWebSocket(Uri URI)
    {
        private readonly ClientWebSocket Base = new();

        public Uri URL { get; } = URI;

        public event Action<string>? OnMessage;
        public event Action? OnOpen;
        public event Action<WebSocketCloseStatus?, string?>? OnClose;
        public event Action<Exception>? OnError;

        public async Task Connect(CancellationToken cancellationToken)
        {
            await this.Base.ConnectAsync(this.URL, cancellationToken);

            _ = Task.Run(ReceiveLoop);
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[4096];

            try
            {
                while (Base.State == WebSocketState.Open)
                {
                    var result = await Base.ReceiveAsync(buffer, CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        OnClose?.Invoke(result.CloseStatus, result.CloseStatusDescription);
                        return;
                    }

                    var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    OnMessage?.Invoke(msg);
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
            }
        }

        public Task SendAsync(string text)
        {
            return Base.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}