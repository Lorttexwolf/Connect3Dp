#nullable enable
namespace Lib3Dp.State;

public readonly record struct NotificationChanges(
	bool IssuedAtHasChanged,
	System.DateTimeOffset? IssuedAtPrevious,
	System.DateTimeOffset? IssuedAtNew,
	bool LastSeenAtHasChanged,
	System.DateTimeOffset? LastSeenAtPrevious,
	System.DateTimeOffset? LastSeenAtNew
)
{
    public bool HasChanged => IssuedAtHasChanged || LastSeenAtHasChanged;

	public override string ToString()
	{
		if (!HasChanged) return $"{nameof(NotificationChanges)}";
		var parts = new List<string>();
		if (IssuedAtHasChanged) 
			parts.Add($"IssuedAt = Previous: {IssuedAtPrevious}, New: {IssuedAtNew}");
		if (LastSeenAtHasChanged) 
			parts.Add($"LastSeenAt = Previous: {LastSeenAtPrevious}, New: {LastSeenAtNew}");
		return $"NotificationChanges {(string.Join(", ", parts))}";
	}
}
