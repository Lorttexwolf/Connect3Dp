namespace JeIdentity
{
	/// <summary>
	/// A discrete feature set or sub-service exposed by this host.
	/// </summary>
	public record ServiceModule(string Name, Version Version, string Description);
}
