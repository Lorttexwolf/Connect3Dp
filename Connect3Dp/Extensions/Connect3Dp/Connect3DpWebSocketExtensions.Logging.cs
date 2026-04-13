using Connect3Dp.Logging;
using Connect3Dp.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Connect3Dp.Extensions.Connect3Dp
{
	public static partial class Connect3DpWebSocketExtensions
	{
		public record LogsToBeSentSettings(BufferedLoggerChannel Channel, Task ForwardTask, CancellationTokenSource Cts);

		public record struct SubscribeToLogsPayload([property: JsonRequired] TimeSpan MinInterval);
		public static WebSocketServer<Connect3DpWebSocketClient> WithSubscribeToLogs(this WebSocketServer<Connect3DpWebSocketClient> ws, BufferedLoggerProvider bufferedLogger)
		{
			ws.MapAction<SubscribeToLogsPayload, WebSocketClientActionResult>(Topics.Logging.Subscribe, async (connection, payload) =>
			{
				StartLogForwarding(ws, connection, bufferedLogger, payload.MinInterval);

				return WebSocketClientActionResult.Success();
			});

			return ws;
		}

		public static WebSocketServer<Connect3DpWebSocketClient> WithUnsubscribeToLogs(this WebSocketServer<Connect3DpWebSocketClient> ws, BufferedLoggerProvider bufferedLogger)
		{
			ws.MapAction(Topics.Logging.Unsubscribe, async (connection) =>
			{
				StopLogForwarding(connection);

				return WebSocketClientActionResult.Success();
			});

			return ws;
		}

		public record struct LogHistoryRetrievalPayload([property: JsonRequired, Range(1, int.MaxValue)] int Max, DateTimeOffset? Before, DateTimeOffset? After, [property: JsonRequired, JsonConverter(typeof(JsonStringEnumConverter))] LogLevel MinLevel);
		public record struct LogHistoryRetrievalResult(bool IsSuccess, string? FailureReason, IEnumerable<ForwardingLogger.Entry>? Entries) : IWebSocketClientActionResult;
		public static WebSocketServer<Connect3DpWebSocketClient> WithLogHistoryRetrieval(this WebSocketServer<Connect3DpWebSocketClient> ws, BufferedLoggerProvider bufferedLogger)
		{
			ws.MapAction<LogHistoryRetrievalPayload, LogHistoryRetrievalResult>(Topics.Logging.History, async (connection, payload) =>
			{
				if (payload.Max < 1)
				{
					return new LogHistoryRetrievalResult(false, "Max count must be greater than zero.", null);
				}

				var entries = bufferedLogger.Entries.AsEnumerable();

				entries = entries.Where(e => e.Level >= payload.MinLevel);

				if (payload.After.HasValue)
					entries = entries.Where(e => e.Time >= payload.After.Value);

				if (payload.Before.HasValue)
					entries = entries.Where(e => e.Time < payload.Before.Value);

				entries = entries.TakeLast(payload.Max);

				return new LogHistoryRetrievalResult(true, null, entries);
			});

			return ws;
		}

		private static void StartLogForwarding(WebSocketServer<Connect3DpWebSocketClient> ws, Connect3DpWebSocketClient sc, BufferedLoggerProvider bufferedLogger, TimeSpan minInterval)
		{
			if (sc.LogBroadcast is not null)
			{
				// TODO: Update settings of logs.
				return;
			}

			var cts = new CancellationTokenSource();
			var channel = bufferedLogger.Subscribe();
			var task = ForwardLogs(ws, channel, minInterval, cts.Token);

			sc.LogBroadcast = new LogsToBeSentSettings(channel, task, cts);
		}

		public static void StopLogForwarding(Connect3DpWebSocketClient sc)
		{
			if (sc.LogBroadcast is not null)
			{
				sc.LogBroadcast.Cts.Cancel();
				sc.LogBroadcast.Channel.Dispose();
				sc.LogBroadcast = null;
			}
		}

		private static async Task ForwardLogs(
			WebSocketServer<Connect3DpWebSocketClient> server,
			BufferedLoggerChannel channel,
			TimeSpan minInterval,
			CancellationToken ct)
		{
			var pending = new List<ForwardingLogger.Entry>();
			var nextSend = DateTimeOffset.UtcNow;

			try
			{
				await foreach (var entry in channel.Reader.ReadAllAsync(ct))
				{
					pending.Add(entry);

					var now = DateTimeOffset.UtcNow;
					if (now < nextSend) continue;
					nextSend = now + minInterval;

					var message = new MessageToClient<IEnumerable<ForwardingLogger.Entry>>(null, DateTimeOffset.UtcNow, Topics.Logging.Logs, pending);

					await WebSocketServer<Connect3DpWebSocketClient>.BroadcastMessageAsync(message, server.Clients.Values.Where(c => c.LogBroadcast is not null));

					pending.Clear();
				}
			}
			catch (OperationCanceledException) { }
		}
	}
}
