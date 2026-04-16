using Lib3Dp.State;
using System.Text;

namespace Lib3Dp.Testing;

/// <summary>
/// Formats <see cref="MachineStateChanges"/> into human-readable diff lines for console output.
/// Only fields that actually changed are included. Returns an empty string when nothing changed.
/// </summary>
public static class StateChangeDiffLogger
{
	public static string Format(in MachineStateChanges changes)
	{
		if (!changes.HasChanged) return string.Empty;

		var sb = new StringBuilder();
		sb.AppendLine($"[STATE CHANGE] {DateTime.Now:HH:mm:ss.fff}");

		// Scalars
		if (changes.StatusHasChanged)
			sb.AppendLine($"  status: {changes.StatusPrevious} → {changes.StatusNew}");
		if (changes.CapabilitiesHasChanged)
			sb.AppendLine($"  capabilities: {changes.CapabilitiesPrevious} → {changes.CapabilitiesNew}");
		if (changes.NicknameHasChanged)
			sb.AppendLine($"  nickname: \"{changes.NicknamePrevious}\" → \"{changes.NicknameNew}\"");
		if (changes.AirDuctModeHasChanged)
			sb.AppendLine($"  airDuctMode: {changes.AirDuctModePrevious} → {changes.AirDuctModeNew}");
		if (changes.IsLocalStorageScanningHasChanged)
			sb.AppendLine($"  isLocalStorageScanning: {changes.IsLocalStorageScanningPrevious} → {changes.IsLocalStorageScanningNew}");
		if (changes.StreamingOMEURLHasChanged)
			sb.AppendLine($"  streamingURL: \"{changes.StreamingOMEURLPrevious}\" → \"{changes.StreamingOMEURLNew}\"");

		// Lights
		foreach (var kv in changes.LightsAdded ?? [])
			sb.AppendLine($"  lights: {kv.Key} added ({(kv.Value ? "on" : "off")})");
		foreach (var k in changes.LightsRemoved ?? [])
			sb.AppendLine($"  lights: {k} removed");
		foreach (var kv in changes.LightsUpdated ?? [])
			sb.AppendLine($"  lights: {kv.Key} → {(kv.Value ? "on" : "off")}");

		// Heating Elements
		foreach (var kv in changes.HeatingElementsAdded ?? [])
			sb.AppendLine($"  heatingElements: {kv.Key} added ({kv.Value.TempC:F1}°C / {kv.Value.TargetTempC:F1}°C target)");
		foreach (var k in changes.HeatingElementsRemoved ?? [])
			sb.AppendLine($"  heatingElements: {k} removed");
		foreach (var kv in changes.HeatingElementsUpdated ?? [])
			sb.AppendLine($"  heatingElements: {kv.Key}: {kv.Value.TempC:F1}°C / {kv.Value.TargetTempC:F1}°C target");

		// Current Job
		if (changes.CurrentJobChanges is { HasChanged: true } jobChanges)
		{
			if (jobChanges.NameHasChanged)
				sb.AppendLine($"  currentJob.name: \"{jobChanges.NamePrevious}\" → \"{jobChanges.NameNew}\"");
			if (jobChanges.PercentageCompleteHasChanged)
				sb.AppendLine($"  currentJob.percentageComplete: {jobChanges.PercentageCompletePrevious}% → {jobChanges.PercentageCompleteNew}%");
			if (jobChanges.RemainingTimeHasChanged)
				sb.AppendLine($"  currentJob.remainingTime: {jobChanges.RemainingTimePrevious} → {jobChanges.RemainingTimeNew}");
			if (jobChanges.SubStageHasChanged)
				sb.AppendLine($"  currentJob.subStage: \"{jobChanges.SubStagePrevious}\" → \"{jobChanges.SubStageNew}\"");
			if (jobChanges.IssueHasChanged)
				sb.AppendLine($"  currentJob.issue: {jobChanges.IssuePrevious?.Title ?? "(none)"} → {jobChanges.IssueNew?.Title ?? "(none)"}");
		}

		// Notifications
		foreach (var kv in changes.NotificationsAdded ?? [])
			sb.AppendLine($"  notification added: [{kv.Key}] {kv.Value.Message.Title}");
		foreach (var k in changes.NotificationsRemoved ?? [])
			sb.AppendLine($"  notification removed: {k}");

		// Local jobs summary
		if (changes.LocalJobsAdded?.Length > 0)
			sb.AppendLine($"  localJobs: +{changes.LocalJobsAdded.Length} file(s)");
		if (changes.LocalJobsRemoved?.Length > 0)
			sb.AppendLine($"  localJobs: -{changes.LocalJobsRemoved.Length} file(s)");

		return sb.ToString().TrimEnd();
	}
}
