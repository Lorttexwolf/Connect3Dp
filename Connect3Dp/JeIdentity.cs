using JeIdentity;

namespace Connect3Dp
{
	public class JeIdentity : IJeIdentityProvider
	{
		static readonly ServiceIdentity Identity;

		static JeIdentity()
		{
			Identity = new ServiceIdentity("Connect3Dp", Version.Parse(VersionInfo.Version), "Composable HTTP and WebSocket controllers for Lib3Dp", [
			
				Lib3Dp.JeIdentity.GetServiceIdentity().IntoModule()

			]);
		}

		public static ServiceIdentity GetServiceIdentity()
		{
			return Identity;
		}
	}
}
