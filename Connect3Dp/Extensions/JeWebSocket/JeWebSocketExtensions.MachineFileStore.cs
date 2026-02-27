using Connect3Dp.Services;
using Lib3Dp;
using Lib3Dp.Files;

namespace Connect3Dp.Extensions.JeWebSocket
{
	public static partial class JeWebSocketExtensions
	{
		public record struct MachineFileStoreMachineUsagePayload(string MachineID) : IMachineSpecificPayload;
		public record struct MachineFileStoreTotalUsageResult(bool IsSuccess, string? FailureReason, StorageInfo? TotalUsage) : IJeWebSocketClientActionResult;
		public record struct MachineFileStoreMachineUsageResult(bool IsSuccess, string? FailureReason, string? MachineID, StorageInfo? MachineUsage) : IJeWebSocketClientActionResult;

		public static JeWebSocketServer<JeWebSocketClientForConnect3Dp> WithMachineFileStoreTotalUsage(this JeWebSocketServer<JeWebSocketClientForConnect3Dp> ws, IMachineFileStore fileStore)
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

		public static JeWebSocketServer<JeWebSocketClientForConnect3Dp> WithMachineFileStoreMachineUsage(this JeWebSocketServer<JeWebSocketClientForConnect3Dp> ws, MachineConnectionCollection machineCollection, IMachineFileStore fileStore)
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
