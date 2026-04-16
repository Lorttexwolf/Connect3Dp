using Connect3Dp.Services;
using Lib3Dp;
using Lib3Dp.Connectors;
using Lib3Dp.Extensions;
using Lib3Dp.Files;
using Lib3Dp.State;
using System.Text.Json.Serialization;

namespace Connect3Dp.Extensions.Connect3Dp
{
	public static partial class Connect3DpWebSocketExtensions
	{
		public record struct SubscribeToMachinePayload(string MachineID, [property: JsonRequired] Connect3DpWebSocketClient.StateDetails DetailOfState) : IMachineSpecificPayload;
		public record struct UnsubscribeFromMachinePayload(string MachineID) : IMachineSpecificPayload;
		public record struct SubscribeActionResult(MachineState? FullState, AtAGlanceMachineState? AtAGlanceState, string? FailureReason) : IWebSocketClientActionResult
		{
			public readonly bool IsSuccess => FailureReason is null;
		}

		public static WebSocketServer<Connect3DpWebSocketClient> WithSubscribeAndUnsubscribeAction(
			this WebSocketServer<Connect3DpWebSocketClient> ws,
			MachineConnectionCollection machineCollection)
		{
			ws.MapMachineSpecificAction<SubscribeToMachinePayload, SubscribeActionResult>(machineCollection, Topics.Machine.Subscribe, (connection, payload, machine) =>
			{
				if (connection.MachineSubscriptions.TryGetValue(connection.ID, out var existing))
					existing.DetailOfState = payload.DetailOfState;
				else
					connection.MachineSubscriptions.TryAdd(machine.ID, new Connect3DpWebSocketClient.MachineSubscription { DetailOfState = payload.DetailOfState });

				var result = payload.DetailOfState switch
				{
					Connect3DpWebSocketClient.StateDetails.Full => new SubscribeActionResult((MachineState)machine.State, null, null),
					Connect3DpWebSocketClient.StateDetails.AtAGlance => new SubscribeActionResult(null, AtAGlanceMachineState.Of(machine.State), null),
					_ => new SubscribeActionResult(null, null, null)
				};

				return Task.FromResult(result);
			});

			ws.MapMachineSpecificAction<UnsubscribeFromMachinePayload, WebSocketClientActionResult>(machineCollection, Topics.Machine.Unsubscribe, (connection, payload, _) =>
			{
				connection.MachineSubscriptions.Remove(payload.MachineID);
				return Task.FromResult(WebSocketClientActionResult.Success());
			});

			return ws;
		}

		public record struct BroadcastedMachineStateUpdateData(MachineStateChanges? FullChanges, AtAGlanceMachineStateChanges? AtAGlanceChanges);

		public static WebSocketServer<Connect3DpWebSocketClient> WithStateBroadcasts(
			this WebSocketServer<Connect3DpWebSocketClient> ws,
			MachineConnectionCollection machineCollection)
		{
			machineCollection.OnStateChange += async (_, ev) =>
			{
				var now = DateTimeOffset.UtcNow;

				var fullClients = ws.Clients.Values
					.Where(c => c.MachineSubscriptions.TryGetValue(ev.MachineID, out var sub) && sub.DetailOfState is Connect3DpWebSocketClient.StateDetails.Full)
					.ToList();

				var atAGlanceClients = ws.Clients.Values
					.Where(c => c.MachineSubscriptions.TryGetValue(ev.MachineID, out var sub) && sub.DetailOfState is Connect3DpWebSocketClient.StateDetails.AtAGlance)
					.ToList();

				if (fullClients.Count != 0)
					await WebSocketServer<Connect3DpWebSocketClient>.BroadcastMessageAsync(
						new MessageToClient<BroadcastedMachineStateUpdateData>(null, now, Topics.StateUpdated(ev.MachineID), new(ev.Changes, null)),
						fullClients);

				var atAGlanceChanges = AtAGlanceMachineStateChanges.Of(ev.Changes);
				if (atAGlanceClients.Count != 0 && atAGlanceChanges.HasChanged)
					await WebSocketServer<Connect3DpWebSocketClient>.BroadcastMessageAsync(
						new MessageToClient<BroadcastedMachineStateUpdateData>(null, now, Topics.StateUpdated(ev.MachineID), new(null, atAGlanceChanges)),
						atAGlanceClients);
			};

			return ws;
		}

		public record struct MarkAsIdlePayload(string MachineID) : IMachineSpecificPayload;
		public record struct PauseMachinePayload(string MachineID) : IMachineSpecificPayload;
		public record struct ResumeMachinePayload(string MachineID) : IMachineSpecificPayload;
		public record struct StopMachinePayload(string MachineID) : IMachineSpecificPayload;
		public record struct ToggleLightMachinePayload(string MachineID, string FixtureName, bool IsOn) : IMachineSpecificPayload;
		public record struct SetFanSpeedMachinePayload(string MachineID, string FanName, int SpeedPercent) : IMachineSpecificPayload;
		public record struct StartPrintPayload(string MachineID, MachineFileHandle File, PrintOptions Options) : IMachineSpecificPayload;

		public static WebSocketServer<Connect3DpWebSocketClient> WithMarkAsIdleAction(
			this WebSocketServer<Connect3DpWebSocketClient> ws, MachineConnectionCollection machineCollection) =>
			ws.MapMachineSpecificAction<MarkAsIdlePayload, ClientMessageMachineOperationResult>(machineCollection, Topics.Machine.MarkAsIdle,
				async (_, _, machine) => ClientMessageMachineOperationResult.Of(await machine.MarkAsIdle()));

		public static WebSocketServer<Connect3DpWebSocketClient> WithPauseMachine(
			this WebSocketServer<Connect3DpWebSocketClient> ws, MachineConnectionCollection machineCollection) =>
			ws.MapMachineSpecificAction<PauseMachinePayload, ClientMessageMachineOperationResult>(machineCollection, Topics.Machine.Pause,
				async (_, _, machine) => ClientMessageMachineOperationResult.Of(await machine.Pause()));

		public static WebSocketServer<Connect3DpWebSocketClient> WithResumeMachine(
			this WebSocketServer<Connect3DpWebSocketClient> ws, MachineConnectionCollection machineCollection) =>
			ws.MapMachineSpecificAction<ResumeMachinePayload, ClientMessageMachineOperationResult>(machineCollection, Topics.Machine.Resume,
				async (_, _, machine) => ClientMessageMachineOperationResult.Of(await machine.Resume()));

		public static WebSocketServer<Connect3DpWebSocketClient> WithStopMachine(
			this WebSocketServer<Connect3DpWebSocketClient> ws, MachineConnectionCollection machineCollection) =>
			ws.MapMachineSpecificAction<StopMachinePayload, ClientMessageMachineOperationResult>(machineCollection, Topics.Machine.Stop,
				async (_, _, machine) => ClientMessageMachineOperationResult.Of(await machine.Stop()));

		public static WebSocketServer<Connect3DpWebSocketClient> WithToggleLightMachine(
			this WebSocketServer<Connect3DpWebSocketClient> ws, MachineConnectionCollection machineCollection) =>
			ws.MapMachineSpecificAction<ToggleLightMachinePayload, ClientMessageMachineOperationResult>(machineCollection, Topics.Machine.ToggleLight,
				async (_, payload, machine) => ClientMessageMachineOperationResult.Of(await machine.ToggleLight(payload.FixtureName, payload.IsOn)));

		public static WebSocketServer<Connect3DpWebSocketClient> WithSetFanSpeedMachine(
			this WebSocketServer<Connect3DpWebSocketClient> ws, MachineConnectionCollection machineCollection) =>
			ws.MapMachineSpecificAction<SetFanSpeedMachinePayload, ClientMessageMachineOperationResult>(machineCollection, Topics.Machine.SetFanSpeed,
				async (_, payload, machine) => ClientMessageMachineOperationResult.Of(await machine.SetFanSpeed(payload.FanName, payload.SpeedPercent)));

		public static WebSocketServer<Connect3DpWebSocketClient> WithStartPrintMachine(
			this WebSocketServer<Connect3DpWebSocketClient> ws, MachineConnectionCollection machineCollection) =>
			ws.MapMachineSpecificAction<StartPrintPayload, ClientMessageMachineOperationResult>(machineCollection, Topics.Machine.StartPrint,
				async (_, payload, machine) =>
				{
					var job = machine.State.LocalJobs.FirstOrDefault(j => j.File == payload.File);
					if (job == default)
						return new ClientMessageMachineOperationResult(false, $"No local job found matching file handle '{payload.File.URI}'", null);
					return ClientMessageMachineOperationResult.Of(await machine.PrintLocal(job, payload.Options));
				});

		public record struct FindMatchingSpoolsPayload(string MachineID, IDictionary<int, MaterialToPrint> MaterialsToPrint) : IMachineSpecificPayload;
		public record struct FindMatchingSpoolsResult(Matches<int, SpoolMatch> Matches) : IWebSocketClientActionResult
		{
			public readonly bool IsSuccess => true;
			public readonly string? FailureReason => null;
		}

		public static WebSocketServer<Connect3DpWebSocketClient> WithFindMatchingSpoolsMachineAction(
			this WebSocketServer<Connect3DpWebSocketClient> ws, MachineConnectionCollection machineCollection) =>
			ws.MapMachineSpecificAction<FindMatchingSpoolsPayload, FindMatchingSpoolsResult>(machineCollection, Topics.Machine.FindMatchingSpools,
				(_, payload, machine) => Task.FromResult(new FindMatchingSpoolsResult(machine.State.FindMatchingSpools(payload.MaterialsToPrint))));

		public record struct ChangeMaterialPayload(string MachineID, SpoolLocation Location, Material Material) : IMachineSpecificPayload;

		public static WebSocketServer<Connect3DpWebSocketClient> WithChangeMaterialMachine(
			this WebSocketServer<Connect3DpWebSocketClient> ws, MachineConnectionCollection machineCollection) =>
			ws.MapMachineSpecificAction<ChangeMaterialPayload, ClientMessageMachineOperationResult>(machineCollection, Topics.Machine.ChangeMaterial,
				async (_, payload, machine) => ClientMessageMachineOperationResult.Of(await machine.ChangeMaterial(payload.Location, payload.Material)));

		public interface IMachineSpecificPayload
		{
			[JsonRequired]
			public string MachineID { get; }
		}

		public static WebSocketServer<Connect3DpWebSocketClient> MapMachineSpecificAction<TPayload, TResult>(
			this WebSocketServer<Connect3DpWebSocketClient> ws,
			MachineConnectionCollection machineCollection,
			string action,
			Func<Connect3DpWebSocketClient, TPayload, MachineConnection, Task<TResult>> handler)
			where TPayload : IMachineSpecificPayload
			where TResult : IWebSocketClientActionResult
		{
			ws.MapAction<TPayload, IWebSocketClientActionResult>(action, async (connection, payload) =>
			{
				if (!machineCollection.Connections.TryGetValue(payload.MachineID, out var machine))
					return WebSocketClientActionResult.Failure($"Could not find Machine with ID {payload.MachineID}");

				return await handler(connection, payload, machine);
			});

			return ws;
		}
	}
}
