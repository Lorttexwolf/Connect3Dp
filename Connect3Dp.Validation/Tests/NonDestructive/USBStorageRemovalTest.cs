using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.State;
using Spectre.Console;

namespace Connect3Dp.Validation.Tests.NonDestructive;

public class USBStorageRemovalTest : ValidationTest
{
	public override string Name => "USB/SD Storage Removal";
	public override string Description => "Remove and reinsert USB/SD storage to verify capability toggling and notifications";
	public override RiskTier Tier => RiskTier.NonDestructive;

	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

	public override async Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		if (!spec.RequiresSDOrUSB)
			return TestResult.Skip("Model does not require SD/USB storage");

		bool hasLocalJobs = connection.State.Capabilities.HasFlag(MachineCapabilities.StartLocalJob);

		if (!hasLocalJobs)
			return TestResult.Fail("StartLocalJob capability is not present before the test — cannot verify removal");

		// Phase 1: Ask user to remove storage
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[bold yellow]Please REMOVE the USB drive or SD card from the printer.[/]");

		bool removed = await WaitForCondition(
			() => !connection.State.Capabilities.HasFlag(MachineCapabilities.StartLocalJob),
			Timeout, ct);

		if (!removed)
			return TestResult.Fail("StartLocalJob capability was not removed within timeout after storage removal");

		// Verify notification appeared
		bool hasNotification = connection.State.MappedNotifications.ContainsKey("bbl.sdcard.missing");

		// Phase 2: Ask user to reinsert storage
		AnsiConsole.MarkupLine("[bold green]Please REINSERT the USB drive or SD card.[/]");

		bool reinserted = await WaitForCondition(
			() => connection.State.Capabilities.HasFlag(MachineCapabilities.StartLocalJob),
			Timeout, ct);

		if (!reinserted)
			return TestResult.Fail("StartLocalJob capability was not restored within timeout after storage reinsertion");

		bool notificationCleared = !connection.State.MappedNotifications.ContainsKey("machine.sdcard.missing");

		string detail = $"Notification on removal: {(hasNotification ? "yes" : "NO")}, cleared on reinsert: {(notificationCleared ? "yes" : "NO")}";

		if (!hasNotification || !notificationCleared)
			return TestResult.Fail("Capabilities toggled correctly but notification behavior was unexpected", detail);

		return TestResult.Pass($"Removal/reinsertion OK. {detail}");
	}

	private static async Task<bool> WaitForCondition(Func<bool> condition, TimeSpan timeout, CancellationToken ct)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();

		while (sw.Elapsed < timeout)
		{
			if (condition())
				return true;

			await Task.Delay(500, ct);
		}

		return false;
	}
}
