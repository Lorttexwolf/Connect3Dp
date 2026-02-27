using Connect3Dp.Services;
using Lib3Dp;
using Lib3Dp.Connectors;
using Lib3Dp.Extensions;
using Lib3Dp.State;
using System.Text.Json.Serialization;

namespace Connect3Dp.Extensions.JeWebSocket
{
	public static partial class JeWebSocketExtensions
	{
		public record struct SubscribeToMachinePayload(string MachineID, [property: JsonRequired] JeWebSocketClientForConnect3Dp.StateDetails DetailOfState) : IMachineSpecificPayload;
		public record struct UnsubscribeFromMachinePayload(string MachineID) : IMachineSpecificPayload;
		public record struct SubscribeActionResult(IMachineState? FullState, AtAGlanceMachineState? AtAGlanceState, string? FailureReason) : IJeWebSocketClientActionResult
		{
			public readonly bool IsSuccess => true;
		}

		public static JeWebSocketServer<JeWebSocketClientForConnect3Dp> WithSubscribeAndUnsubscribeAction(
			this JeWebSocketServer<JeWebSocketClientForConnect3Dp> ws,
			MachineConnectionCollection machineCollection)
		{
			ws.MapMachineSpecificAction<SubscribeToMachinePayload, SubscribeActionResult>(machineCollection, Topics.Machine.Subscribe, (connection, payload, machine) =>
			{
				if (connection.MachineSubscriptions.TryGetValue(connection.ID, out var existing))
					existing.DetailOfState = payload.DetailOfState;
				else
					connection.MachineSubscriptions.TryAdd(machine.ID, new JeWebSocketClientForConnect3Dp.MachineSubscription { DetailOfState = payload.DetailOfState });

				var result = payload.DetailOfState switch
				{
					JeWebSocketClientForConnect3Dp.StateDetails.Full => new SubscribeActionResult(machine.State, null, null),
					JeWebSocketClientForConnect3Dp.StateDetails.AtAGlance => new SubscribeActionResult(null, AtAGlanceMachineState.Of(machine.State), null),
					_ => new SubscribeActionResult(null, null, null)
				};

				return Task.FromResult(result);
			});

			ws.MapMachineSpecificAction<UnsubscribeFromMachinePayload, JeWebSocketClientActionResult>(machineCollection, Topics.Machine.Unsubscribe, (connection, payload, _) =>
			{
				connection.MachineSubscriptions.Remove(payload.MachineID);
				return Task.FromResult(JeWebSocketClientActionResult.Success());
			});

			return ws;
		}

		public record struct BroadcastedMachineStateUpdateData(MachineStateChanges? FullChanges, AtAGlanceMachineStateChanges? AtAGlanceChanges);

		public static JeWebSocketServer<JeWebSocketClientForConnect3Dp> WithStateBroadcasts(
			this JeWebSocketServer<JeWebSocketClientForConnect3Dp> ws,
			MachineConnectionCollection machineCollection)
		{
			machineCollection.OnStateChange += async (_, ev) =>
			{
				var now = DateTimeOffset.UtcNow;

				var fullClients = ws.Clients.Values
					.Where(c => c.MachineSubscriptions.TryGetValue(ev.MachineID, out var sub) && sub.DetailOfState is JeWebSocketClientForConnect3Dp.StateDetails.Full)
					.ToList();

				var atAGlanceClients = ws.Clients.Values
					.Where(c => c.MachineSubscriptions.TryGetValue(ev.MachineID, out var sub) && sub.DetailOfState is JeWebSocketClientForConnect3Dp.StateDetails.AtAGlance)
					.ToList();

				if (fullClients.Count != 0)
					await JeWebSocketServer<JeWebSocketClientForConnect3Dp>.BroadcastMessageAsync(
						new MessageToClient<BroadcastedMachineStateUpdateData>(null, now, Topics.StateUpdated(ev.MachineID), new(ev.Changes, null)),
						fullClients);

				var atAGlanceChanges = AtAGlanceMachineStateChanges.Of(ev.Changes);
				if (atAGlanceClients.Count != 0 && atAGlanceChanges.HasChanged)
					await JeWebSocketServer<JeWebSocketClientForConnect3Dp>.BroadcastMessageAsync(
						new MessageToClient<BroadcastedMachineStateUpdateData>(null, now, Topics.StateUpdated(ev.MachineID), new(null, atAGlanceChanges)),
						atAGlanceClients);
			};

			return ws;
		}

		public record struct MarkAsIdlePayload(string MachineID) : IMachineSpecificPayload;
		public record struct PauseMachinePayload(string MachineID) : IMachineSpecificPayload;
		public record struct ResumeMachinePayload(string MachineID) : IMachineSpecificPayload;
		public record struct StopMachinePayload(string MachineID) : IMachineSpecificPayload;

		public static JeWebSocketServer<JeWebSocketClientForConnect3Dp> WithMarkAsIdleAction(
			this JeWebSocketServer<JeWebSocketClientForConnect3Dp> ws, MachineConnectionCollection machineCollection) =>
			ws.MapMachineSpecificAction<MarkAsIdlePayload, ClientMessageMachineOperationResult>(machineCollection, Topics.Machine.MarkAsIdle,
				async (_, _, machine) => ClientMessageMachineOperationResult.Of(await machine.MarkAsIdle()));

		public static JeWebSocketServer<JeWebSocketClientForConnect3Dp> WithPauseMachine(
			this JeWebSocketServer<JeWebSocketClientForConnect3Dp> ws, MachineConnectionCollection machineCollection) =>
			ws.MapMachineSpecificAction<PauseMachinePayload, ClientMessageMachineOperationResult>(machineCollection, Topics.Machine.Pause,
				async (_, _, machine) => ClientMessageMachineOperationResult.Of(await machine.Pause()));

		public static JeWebSocketServer<JeWebSocketClientForConnect3Dp> WithResumeMachine(
			this JeWebSocketServer<JeWebSocketClientForConnect3Dp> ws, MachineConnectionCollection machineCollection) =>
			ws.MapMachineSpecificAction<ResumeMachinePayload, ClientMessageMachineOperationResult>(machineCollection, Topics.Machine.Resume,
				async (_, _, machine) => ClientMessageMachineOperationResult.Of(await machine.Resume()));

		public static JeWebSocketServer<JeWebSocketClientForConnect3Dp> WithStopMachine(
			this JeWebSocketServer<JeWebSocketClientForConnect3Dp> ws, MachineConnectionCollection machineCollection) =>
			ws.MapMachineSpecificAction<StopMachinePayload, ClientMessageMachineOperationResult>(machineCollection, Topics.Machine.Stop,
				async (_, _, machine) => ClientMessageMachineOperationResult.Of(await machine.Stop()));

		public record struct FindMatchingSpoolsPayload(string MachineID, IDictionary<int, MaterialToPrint> MaterialsToPrint) : IMachineSpecificPayload;
		public record struct FindMatchingSpoolsResult(Matches<int, SpoolMatch> Matches) : IJeWebSocketClientActionResult
		{
			public readonly bool IsSuccess => true;
			public readonly string? FailureReason => null;
		}

		public static JeWebSocketServer<JeWebSocketClientForConnect3Dp> WithFindMatchingSpoolsMachineAction(
			this JeWebSocketServer<JeWebSocketClientForConnect3Dp> ws, MachineConnectionCollection machineCollection) =>
			ws.MapMachineSpecificAction<FindMatchingSpoolsPayload, FindMatchingSpoolsResult>(machineCollection, Topics.Machine.FindMatchingSpools,
				(_, payload, machine) => Task.FromResult(new FindMatchingSpoolsResult(machine.State.FindMatchingSpools(payload.MaterialsToPrint))));

		public interface IMachineSpecificPayload
		{
			[JsonRequired]
			public string MachineID { get; }
		}

		public static JeWebSocketServer<JeWebSocketClientForConnect3Dp> MapMachineSpecificAction<TPayload, TResult>(
			this JeWebSocketServer<JeWebSocketClientForConnect3Dp> ws,
			MachineConnectionCollection machineCollection,
			string action,
			Func<JeWebSocketClientForConnect3Dp, TPayload, MachineConnection, Task<TResult>> handler)
			where TPayload : IMachineSpecificPayload
			where TResult : IJeWebSocketClientActionResult
		{
			ws.MapAction<TPayload, IJeWebSocketClientActionResult>(action, async (connection, payload) =>
			{
				if (!machineCollection.Connections.TryGetValue(payload.MachineID, out var machine))
					return JeWebSocketClientActionResult.Failure($"Could not find Machine with ID {payload.MachineID}");

				return await handler(connection, payload, machine);
			});

			return ws;
		}
	}
}
