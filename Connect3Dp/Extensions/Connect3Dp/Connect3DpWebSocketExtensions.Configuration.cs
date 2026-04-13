using Connect3Dp.Services;
using Lib3Dp;
using System.Collections.Immutable;

namespace Connect3Dp.Extensions.Connect3Dp
{
	public static partial class Connect3DpWebSocketExtensions
	{
		public record struct MachinesConfigurationsResult(ImmutableDictionary<string, object> Configurations) : IWebSocketClientActionResult
		{
			public readonly bool IsSuccess => true;
			public readonly string? FailureReason => null;
		}

		public static WebSocketServer<Connect3DpWebSocketClient> WithConfigurationsAction(this WebSocketServer<Connect3DpWebSocketClient> ws, MachineConnectionCollection machineCollection)
		{
			ws.MapAction(Topics.Machine.Configurations.All, (connection) => Task.FromResult(
				new MachinesConfigurationsResult(machineCollection.Connections.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.GetConfiguration()))));

			return ws;
		}
	}
}
