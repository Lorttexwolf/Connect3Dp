using Connect3Dp.Logging;
using Connect3Dp.Services;
using Lib3Dp.State;
using System.Net.WebSockets;
using System.Text.Json.Serialization;

namespace Connect3Dp.Extensions.JeWebSocket
{
	public partial class JeWebSocketExtensions
	{
		public class JeWebSocketClientForConnect3Dp(string id, WebSocket socket) : IJeWebSocketClient
		{
			public string ID { get; } = id;
			public WebSocket WebSocket { get; } = socket;
			public LogsToBeSentSettings? LogBroadcast { get; set; }
			public Dictionary<string, MachineSubscription> MachineSubscriptions { get; } = [];

			public record MachineSubscription
			{
				public StateDetails DetailOfState { get; set; }
			}

			[JsonConverter(typeof(JsonStringEnumConverter))]
			public enum StateDetails
			{
				None,
				/// <summary>
				/// Only <see cref="BroadcastedMachineStateUpdateData.AtAGlanceChanges"/> will be included on state update events.
				/// </summary>
				AtAGlance,
				/// <summary>
				/// Includes the entirety of <see cref="IMachineState"/> and <see cref="MachineStateChanges"/>.
				/// </summary>
				Full
			}

			private bool IsDisposed;

			public void Dispose()
			{
				if (IsDisposed) return;
				IsDisposed = true;

				if (LogBroadcast is { } logs)
				{
					logs.Cts.Cancel();
					logs.Cts.Dispose();
					logs.Channel?.Dispose();
				}

				WebSocket.Dispose();

				GC.SuppressFinalize(this);
			}
		}
	}
}
