using Lib3Dp.State;
using Lib3Dp.Utilities;
using static Lib3Dp.MonoMachine;

namespace Lib3Dp
{
	public readonly record struct MachineOperationResult
	{
		public required bool Success { get; init; }
		public required MachineMessage? Reasoning { get; init; }

		public override string ToString() => Success ? "Success" : $"Failure, {Reasoning}";

		public void ThrowIfFailed()
		{
			if (!Success) throw new Exception(ToString());
		}

		public static MachineOperationResult Ok => new() { Success = true, Reasoning = null };
		public static MachineOperationResult Fail(MachineMessage reasoning) => new() { Success = false, Reasoning = reasoning with { Severity = MachineMessageSeverity.Error } };
		public static MachineOperationResult Fail(string title, string body, MachineMessageActions manualResolve = default, MachineMessageAutoResole autoResolve = default) => Fail(new MachineMessage(title, body, MachineMessageSeverity.Error, manualResolve, autoResolve));
	}

	public static class MutationResultMachineOperationResultExtensions
	{
		internal static MachineOperationResult IntoOperationResult(this in MutationResult mutationResult, string operationFailedTitle, MachineMessageActions manualResolve = default, MachineMessageAutoResole autoResolve = default)
		{
			if (mutationResult.IsSuccess) return MachineOperationResult.Ok;

			string bodyReason;

			if (mutationResult.TimedOut) bodyReason = "Timed out";
			else if (mutationResult.InvokeException != null) bodyReason = "Exception occurred while invoking mutation";
			else bodyReason = "Unknown";

			return MachineOperationResult.Fail(operationFailedTitle, bodyReason, manualResolve, autoResolve);
		}
	}

	public static class LoggerMachineOperationResultExtensions
	{
		public static void Log(this Logger logger, MachineOperationResult operationResult, string operationName)
		{
			if (operationResult.Success) logger.TraceSuccess($"Success: {operationName}");
			else logger.Log(Logger.Level.Error, $"Failed {operationName}: {operationResult.Reasoning}");
		}
	}
}
