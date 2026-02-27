#nullable enable
namespace Lib3Dp.State;

public sealed class NotificationUpdate
{
    public System.DateTimeOffset IssuedAt { get; private set; }
    public bool IssuedAtIsSet { get; private set; }
    public System.DateTimeOffset LastSeenAt { get; private set; }
    public bool LastSeenAtIsSet { get; private set; }

public NotificationUpdate SetIssuedAt(System.DateTimeOffset value)
{
	IssuedAtIsSet = true;
	IssuedAt = value;
	return this;
}

public NotificationUpdate RemoveIssuedAt()
{
	IssuedAtIsSet = true;
	IssuedAt = default;
	return this;
}
public NotificationUpdate UnsetIssuedAt()
{
	IssuedAtIsSet = false;
	IssuedAt = default;
	return this;
}
public NotificationUpdate SetLastSeenAt(System.DateTimeOffset value)
{
	LastSeenAtIsSet = true;
	LastSeenAt = value;
	return this;
}

public NotificationUpdate RemoveLastSeenAt()
{
	LastSeenAtIsSet = true;
	LastSeenAt = default;
	return this;
}
public NotificationUpdate UnsetLastSeenAt()
{
	LastSeenAtIsSet = false;
	LastSeenAt = default;
	return this;
}
    public bool TryCreate(out Notification? outResult)
    {
		outResult = null;
        if (!IssuedAtIsSet) return false;
        if (!LastSeenAtIsSet) return false;
        var result = new Notification() { IssuedAt = this.IssuedAt, LastSeenAt = this.LastSeenAt };
        AppendUpdate(result, out _);
		outResult = result;

        return true;
    }
    public NotificationChanges Changes(Notification notification)
    {
		if (notification == null) return default;
		var __IssuedAt_hasChanged = false;
		System.DateTimeOffset? __IssuedAt_prev = null;
		System.DateTimeOffset? __IssuedAt_new = null;
		if (this.IssuedAtIsSet)
		{
			if (!EqualityComparer<System.DateTimeOffset>.Default.Equals(notification.IssuedAt, this.IssuedAt))
			{
				__IssuedAt_hasChanged = true;
				__IssuedAt_prev = notification.IssuedAt;
				__IssuedAt_new = this.IssuedAt;
			}
		}

		var __LastSeenAt_hasChanged = false;
		System.DateTimeOffset? __LastSeenAt_prev = null;
		System.DateTimeOffset? __LastSeenAt_new = null;
		if (this.LastSeenAtIsSet)
		{
			if (!EqualityComparer<System.DateTimeOffset>.Default.Equals(notification.LastSeenAt, this.LastSeenAt))
			{
				__LastSeenAt_hasChanged = true;
				__LastSeenAt_prev = notification.LastSeenAt;
				__LastSeenAt_new = this.LastSeenAt;
			}
		}

		return new NotificationChanges(__IssuedAt_hasChanged, __IssuedAt_prev, __IssuedAt_new, __LastSeenAt_hasChanged, __LastSeenAt_prev, __LastSeenAt_new);
    }

    public void AppendUpdate(Notification notification, out NotificationChanges changes)
    {
		changes = Changes(notification);


		if (this.LastSeenAtIsSet)
		{
			notification.LastSeenAt = this.LastSeenAt;
		}

    }

}
