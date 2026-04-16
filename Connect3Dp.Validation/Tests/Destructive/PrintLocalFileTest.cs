using Connect3Dp.Validation.Specs;
using Lib3Dp.Connectors;
using Lib3Dp.Extensions;
using Lib3Dp.State;

namespace Connect3Dp.Validation.Tests.Destructive;

public class PrintLocalFileTest : ValidationTest
{
	public override string Name => "Print Lifecycle";
	public override string Description => "Start validation print, wait for completion, verify history, then reprint";
	public override RiskTier Tier => RiskTier.Destructive;

	public override async Task<TestResult> RunAsync(MachineConnection connection, ModelSpec spec, CancellationToken ct)
	{
		if (!connection.State.Capabilities.HasFlag(MachineCapabilities.StartLocalJob))
			return TestResult.Skip("StartLocalJob not present");

		if (!connection.State.Capabilities.HasFlag(MachineCapabilities.LocalJobs))
			return TestResult.Skip("LocalJobs not present");

		while (connection.State.IsLocalStorageScanning)
			await Task.Delay(500, ct);

		if (string.IsNullOrEmpty(spec.ValidationPrintFileName))
			return TestResult.Skip("No validation print file in spec");

		// Ensure idle
		if (connection.State.Status is MachineStatus.Printed or MachineStatus.Canceled)
		{
			await connection.MarkAsIdle();
			if (!await WaitForStatus(connection, MachineStatus.Idle, TimeSpan.FromSeconds(10), ct))
				return TestResult.Fail("Could not return to idle", $"Status: {connection.State.Status}");
		}

		if (connection.State.Status != MachineStatus.Idle)
			return TestResult.Fail("Not idle", $"Status: {connection.State.Status}");

		// Find validation file
		var job = connection.State.LocalJobs.FirstOrDefault(j =>
			j.Name.Contains(spec.ValidationPrintFileName, StringComparison.OrdinalIgnoreCase));

		if (job.Name == null)
			return TestResult.Fail($"'{spec.ValidationPrintFileName}' not found on printer");

		// Match materials
		var matches = connection.State.FindMatchingSpools(job.MaterialsToPrint);
		if (matches.HasMissing)
			return TestResult.Fail($"{matches.Missing.Count} material(s) could not be matched to a loaded slot");

		var materialMap = matches.Match.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Location);
		var options = new PrintOptions(
			CustomID: null,
			LevelBed: false,
			FlowCalibration: false,
			VibrationCalibration: false,
			InspectFirstLayer: false,
			MaterialMap: materialMap);

		// --- Phase 1: Start print ---
		var historyCountBefore = connection.State.JobHistory.Count();

		var startResult = await connection.PrintLocal(job, options);
		if (!startResult.Success)
			return TestResult.Fail("PrintLocal failed", startResult.Reasoning?.ToString());

		await Task.Delay(10_000, ct);

		if (!await WaitForStatus(connection, MachineStatus.Printing, TimeSpan.FromSeconds(30), ct))
			return TestResult.Fail("Did not start printing within 30s", $"Status: {connection.State.Status}");

		// --- Phase 2: Wait for print to complete ---
		// Use the job's estimated time plus a generous buffer
		var maxWait = job.Time + job.Time + TimeSpan.FromMinutes(5);

		if (!await WaitForAnyStatus(connection, [MachineStatus.Printed, MachineStatus.Canceled, MachineStatus.Idle], maxWait, ct))
			return TestResult.Fail("Print did not complete within expected time", $"Status: {connection.State.Status}, max wait: {maxWait}");

		if (connection.State.Status == MachineStatus.Canceled)
			return TestResult.Fail("Print was canceled unexpectedly");

		// --- Phase 3: Verify history entry ---
		// Give a moment for history to update
		await Task.Delay(3_000, ct);

		var historyEntry = connection.State.JobHistory
			.FirstOrDefault(h => h.Name.Contains(spec.ValidationPrintFileName, StringComparison.OrdinalIgnoreCase));

		if (historyEntry.Name == null)
			return TestResult.Fail("Print completed but no matching history entry found");

		if (!historyEntry.IsSuccess)
			return TestResult.Fail("History entry exists but marked as failed");

		// --- Phase 4: Mark idle and reprint ---
		if (connection.State.Status is MachineStatus.Printed or MachineStatus.Canceled)
		{
			await connection.MarkAsIdle();
			if (!await WaitForStatus(connection, MachineStatus.Idle, TimeSpan.FromSeconds(15), ct))
				return TestResult.Fail("Could not return to idle for reprint", $"Status: {connection.State.Status}");
		}

		var reprintResult = await connection.PrintLocal(job, options);
		if (!reprintResult.Success)
			return TestResult.Fail("Reprint failed", reprintResult.Reasoning?.ToString());

		await Task.Delay(10_000, ct);

		if (!await WaitForStatus(connection, MachineStatus.Printing, TimeSpan.FromSeconds(30), ct))
			return TestResult.Fail("Reprint did not start within 30s", $"Status: {connection.State.Status}");

		// Stop the reprint — we only needed to verify it starts
		await connection.Stop();

		return TestResult.Pass($"Full lifecycle OK: printed '{job.Name}', verified history, reprint started");
	}

	internal static async Task<bool> WaitForStatus(MachineConnection connection, MachineStatus expected, TimeSpan timeout, CancellationToken ct)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			if (connection.State.Status == expected)
				return true;
			await Task.Delay(500, ct);
		}
		return connection.State.Status == expected;
	}

	internal static async Task<bool> WaitForAnyStatus(MachineConnection connection, MachineStatus[] expected, TimeSpan timeout, CancellationToken ct)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			if (expected.Contains(connection.State.Status))
				return true;
			await Task.Delay(1_000, ct);
		}
		return expected.Contains(connection.State.Status);
	}
}
