using Connect3Dp.Logging;
using Connect3Dp.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Connect3Dp.Extensions.JeWebSocket
{
	public static partial class JeWebSocketExtensions
	{
		public record LogsToBeSentSettings(BufferedLoggerChannel Channel, Task PumpTask, CancellationTokenSource Cts);

		public record struct SubscribeToLogsPayload([property: JsonRequired] TimeSpan MinInterval);
		public static JeWebSocketServer<JeWebSocketClientForConnect3Dp> WithSubscribeToLogs(this JeWebSocketServer<JeWebSocketClientForConnect3Dp> ws, BufferedLoggerProvider bufferedLogger)
		{
			ws.MapAction<SubscribeToLogsPayload, JeWebSocketClientActionResult>(Topics.Logging.Subscribe, async (connection, payload) =>
			{
				SendLogsToClient(ws, connection, bufferedLogger, payload.MinInterval);

				return JeWebSocketClientActionResult.Success();
			});

			return ws;
		}

		public static JeWebSocketServer<JeWebSocketClientForConnect3Dp> WithUnsubscribeToLogs(this JeWebSocketServer<JeWebSocketClientForConnect3Dp> ws, BufferedLoggerProvider bufferedLogger)
		{
			ws.MapAction(Topics.Logging.Unsubscribe, async (connection) =>
			{
				StopSendingLogsToClient(connection);

				return JeWebSocketClientActionResult.Success();
			});

			return ws;
		}

		public record struct LogHistoryRetrievalPayload([property: JsonRequired, Range(1, int.MaxValue)] int Max, DateTimeOffset? Before, DateTimeOffset? After, [property: JsonRequired, JsonConverter(typeof(JsonStringEnumConverter))] LogLevel MinLevel);
		public record struct LogHistoryRetrievalResult(bool IsSuccess, string? FailureReason, IEnumerable<ForwardingLogger.Entry>? Entries) : IJeWebSocketClientActionResult;
		public static JeWebSocketServer<JeWebSocketClientForConnect3Dp> WithLogHistoryRetrieval(this JeWebSocketServer<JeWebSocketClientForConnect3Dp> ws, BufferedLoggerProvider bufferedLogger)
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

				Console.WriteLine(entries.Count());

				return new LogHistoryRetrievalResult(true, null, entries);
			});

			return ws;
		}

		private static void SendLogsToClient(JeWebSocketServer<JeWebSocketClientForConnect3Dp> ws, JeWebSocketClientForConnect3Dp sc, BufferedLoggerProvider bufferedLogger, TimeSpan minInterval)
		{
			if (sc.LogBroadcast is not null)
			{
				// TODO: Update settings of logs.
				return;
			}

			var cts = new CancellationTokenSource();
			var channel = bufferedLogger.Subscribe();
			var task = PumpLogs(ws, channel, minInterval, cts.Token);

			sc.LogBroadcast = new LogsToBeSentSettings(channel, task, cts);
		}

		public static void StopSendingLogsToClient(JeWebSocketClientForConnect3Dp sc)
		{
			if (sc.LogBroadcast is not null)
			{
				sc.LogBroadcast.Cts.Cancel();
				sc.LogBroadcast.Channel.Dispose();
				sc.LogBroadcast = null;
			}
		}

		private static async Task PumpLogs(
			JeWebSocketServer<JeWebSocketClientForConnect3Dp> server,
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

					await JeWebSocketServer<JeWebSocketClientForConnect3Dp>.BroadcastMessageAsync(message, server.Clients.Values.Where(c => c.LogBroadcast is not null));

					pending.Clear();
				}
			}
			catch (OperationCanceledException) { }
		}
	}
}
