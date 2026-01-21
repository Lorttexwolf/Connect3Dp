using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Connect3Dp.Utilities
{
    public sealed class SimpleWebSocketClient : IAsyncDisposable
    {
        private readonly Uri Address;
        private readonly ClientWebSocket Base;

        private readonly SemaphoreSlim SendLock = new(1, 1);
        private readonly SemaphoreSlim ReceiveLock = new(1, 1);

        public WebSocketState State => Base.State;

        public WebSocketCloseStatus? CloseStatus => Base.CloseStatus;
        public string? CloseStatusDescription => Base.CloseStatusDescription;

        /// <summary>
        /// Creates and connects a WebSocket by upgrading an HTTP(S) connection.
        /// </summary>
        /// <param name="uri">URI must use ws:// or wss://</param>
        public SimpleWebSocketClient(Uri uri, Action<ClientWebSocketOptions>? configureOptions = null)
        {
            this.Address = uri;
            this.Base = new ClientWebSocket();

            this.Base.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);

            configureOptions?.Invoke(this.Base.Options);
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            // HTTP to WebSocket upgrade
            await Base.ConnectAsync(this.Address, cancellationToken);
        }

        public async Task SendTextAsync(string message, CancellationToken cancellationToken = default)
        {
            await SendLock.WaitAsync(cancellationToken);

            try
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                var buffer = new ArraySegment<byte>(bytes);

                await Base.SendAsync(
                    buffer,
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken);
            }
            finally
            {
                SendLock.Release();
            }
        }

        public Task SendJsonAsync(object obj, CancellationToken cancellationToken = default)
        {
            return SendTextAsync(JsonSerializer.Serialize(obj), cancellationToken);
        }

        public async Task<string?> ReceiveTextAsync(CancellationToken cancellationToken = default)
        {
            await ReceiveLock.WaitAsync(cancellationToken);

            try
            {
                var buffer = new byte[4096];
                var segment = new ArraySegment<byte>(buffer);

                using var ms = new MemoryStream();

                while (true)
                {
                    var result = await Base.ReceiveAsync(segment, cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await Base.CloseAsync(WebSocketCloseStatus.NormalClosure, "Ack", cancellationToken);

                        return null;
                    }

                    ms.Write(buffer, 0, result.Count);

                    if (result.EndOfMessage) return Encoding.UTF8.GetString(ms.ToArray());
                }
            }
            finally
            {
                ReceiveLock.Release();
            }
        }

        public async Task<JsonDocument?> ReceiveJsonAsync(CancellationToken cancellationToken = default)
        {
            string? text = await ReceiveTextAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(text)) return null;

            return JsonDocument.Parse(text);
        }

        public async Task CloseAsync(WebSocketCloseStatus status = WebSocketCloseStatus.NormalClosure, string? description = null, CancellationToken cancellationToken = default)
        {
            if (Base.State == WebSocketState.Open || Base.State == WebSocketState.CloseReceived)
            {
                await Base.CloseAsync(status, description, cancellationToken);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Base.State == WebSocketState.Open)
            {
                await Base.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disposing", CancellationToken.None);
            }
            Base.Dispose();
        }
    }
}
