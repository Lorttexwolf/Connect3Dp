using Connect3Dp.Services;
using Lib3Dp;
using System.Collections.Immutable;

namespace Connect3Dp.Extensions.JeWebSocket
{
	public static partial class JeWebSocketExtensions
	{
		public record struct MachinesConfigurationsResult(ImmutableDictionary<string, object> Configurations) : IJeWebSocketClientActionResult
		{
			public readonly bool IsSuccess => true;
			public readonly string? FailureReason => null;
		}

		public static JeWebSocketServer<JeWebSocketClientForConnect3Dp> WithConfigurationsAction(this JeWebSocketServer<JeWebSocketClientForConnect3Dp> ws, MachineConnectionCollection machineCollection)
		{
			ws.MapAction(Topics.Machine.Configurations.All, (connection) => Task.FromResult(
				new MachinesConfigurationsResult(machineCollection.Connections.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.GetConfiguration()))));

			return ws;
		}
	}
}
