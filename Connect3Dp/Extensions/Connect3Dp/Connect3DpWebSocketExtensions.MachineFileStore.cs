using Connect3Dp.Services;
using Lib3Dp;
using Lib3Dp.Files;

namespace Connect3Dp.Extensions.Connect3Dp
{
	public static partial class Connect3DpWebSocketExtensions
	{
		public record struct MachineFileStoreMachineUsagePayload(string MachineID) : IMachineSpecificPayload;
		public record struct MachineFileStoreTotalUsageResult(bool IsSuccess, string? FailureReason, StorageInfo? Usage) : IWebSocketClientActionResult;
		public record struct MachineFileStoreMachineUsageResult(bool IsSuccess, string? FailureReason, string? MachineID, StorageInfo? Usage) : IWebSocketClientActionResult;

		public static WebSocketServer<Connect3DpWebSocketClient> WithMachineFileStoreTotalUsage(this WebSocketServer<Connect3DpWebSocketClient> ws, IMachineFileStore fileStore)
		{
			ws.MapAction(Topics.MachineFileStore.TotalUsage, async (connection) =>
			{
				try
				{
					return new MachineFileStoreTotalUsageResult(IsSuccess: true, FailureReason: null, await fileStore.GetStorageInfo());
				}
				catch (Exception ex)
				{
					return new MachineFileStoreTotalUsageResult(IsSuccess: false, FailureReason: ex.Message, null);
				}
			});

			return ws;
		}

		public static WebSocketServer<Connect3DpWebSocketClient> WithMachineFileStoreMachineUsage(this WebSocketServer<Connect3DpWebSocketClient> ws, MachineConnectionCollection machineCollection, IMachineFileStore fileStore)
		{
			ws.MapMachineSpecificAction<MachineFileStoreMachineUsagePayload, MachineFileStoreMachineUsageResult>(machineCollection, Topics.MachineFileStore.MachineUsage, async (connection, payload, _) =>
			{
				try
				{
					return new MachineFileStoreMachineUsageResult(IsSuccess: true, FailureReason: null, payload.MachineID, await fileStore.GetStorageInfo(payload.MachineID));
				}
				catch (Exception ex)
				{
					return new MachineFileStoreMachineUsageResult(IsSuccess: false, FailureReason: ex.Message, payload.MachineID, null);
				}
			});

			return ws;
		}
	}
}
