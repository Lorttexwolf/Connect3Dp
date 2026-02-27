namespace JeIdentity
{
	/// <summary>
	/// Describes this service and its capabilities. Clients use this to determine
	/// compatibility and which features are available before connecting.
	/// </summary>
	public record ServiceIdentity(string Name, Version Version, string Description, ServiceModule[] Modules)
	{
		public ServiceModule IntoModule()
		{
			return new ServiceModule(this.Name, this.Version, this.Description);
		}
	}
}
