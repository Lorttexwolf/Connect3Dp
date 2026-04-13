using System.Net.WebSockets;
using System.Text;

namespace Lib3Dp.Utilities
{
	public sealed class SimpleWebSocketClient : IAsyncDisposable
	{
		private readonly Uri Address;
		private readonly ClientWebSocket Base;

		private readonly SemaphoreSlim SendLock = new(1, 1);
		private readonly CancellationTokenSource ReceiveCts = new();

		private Task? ReceiveLoopTask;

		public WebSocketState State => Base.State;
		public WebSocketCloseStatus? CloseStatus => Base.CloseStatus;
		public string? CloseStatusDescription => Base.CloseStatusDescription;

		public event Action? OnConnected;
		public event Action<WebSocketCloseStatus?, string?>? OnDisconnected;
		public event Action<string>? OnMessage;

		public SimpleWebSocketClient(Uri uri, Action<ClientWebSocketOptions>? configureOptions = null)
		{
			Address = uri;
			Base = new ClientWebSocket
			{
				Options =
				{
					KeepAliveInterval = TimeSpan.FromSeconds(20)
				}
			};
			configureOptions?.Invoke(Base.Options);
		}

		public async Task ConnectAsync(CancellationToken cancellationToken = default)
		{
			await Base.ConnectAsync(Address, cancellationToken);

			OnConnected?.Invoke();

			ReceiveLoopTask = Task.Run(() => ReceiveLoopAsync(ReceiveCts.Token), CancellationToken.None);
		}

		public async Task SendTextAsync(string message, CancellationToken cancellationToken = default)
		{
			await SendLock.WaitAsync(cancellationToken);

			try
			{
				var bytes = Encoding.UTF8.GetBytes(message);

				await Base.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
			}
			finally
			{
				SendLock.Release();
			}
		}

		private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
		{
			var buffer = new byte[4096];

			try
			{
				while (!cancellationToken.IsCancellationRequested && Base.State == WebSocketState.Open)
				{
					using var ms = new MemoryStream();

					while (true)
					{
						var result = await Base.ReceiveAsync(buffer, cancellationToken);

						if (result.MessageType == WebSocketMessageType.Close)
						{
							if (Base.State == WebSocketState.CloseReceived)
							{
								await Base.CloseAsync(WebSocketCloseStatus.NormalClosure, "Ack", cancellationToken);
							}

							OnDisconnected?.Invoke(result.CloseStatus, result.CloseStatusDescription);

							return;
						}

						ms.Write(buffer, 0, result.Count);

						if (result.EndOfMessage) break;
					}

					var text = Encoding.UTF8.GetString(ms.ToArray());

					OnMessage?.Invoke(text);
				}
			}
			catch (OperationCanceledException)
			{

			}
			catch (Exception)
			{
				await CloseInternalAsync(WebSocketCloseStatus.InternalServerError, "Receive Loop Failure", cancellationToken);
			}
		}

		public async Task CloseAsync(WebSocketCloseStatus status = WebSocketCloseStatus.NormalClosure, string? description = null, CancellationToken cancellationToken = default)
		{
			await CloseInternalAsync(status, description, cancellationToken);
		}

		private async Task CloseInternalAsync(WebSocketCloseStatus status, string? description, CancellationToken cancellationToken = default)
		{
			if (Base.State is WebSocketState.Open or WebSocketState.CloseReceived)
			{
				ReceiveCts.Cancel();

				await Base.CloseAsync(status, description, cancellationToken);
			}

			OnDisconnected?.Invoke(status, description);
		}

		public async ValueTask DisposeAsync()
		{
			ReceiveCts.Cancel();

			if (ReceiveLoopTask != null) await ReceiveLoopTask;

			if (Base.State == WebSocketState.Open)
			{
				await Base.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disposing", CancellationToken.None);
			}

			Base.Dispose();
			ReceiveCts.Dispose();
			SendLock.Dispose();
		}
	}
}
