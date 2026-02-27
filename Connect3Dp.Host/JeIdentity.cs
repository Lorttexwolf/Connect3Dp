using JeIdentity;

namespace Connect3Dp.Host
{
	public class JeIdentity : IJeIdentityProvider
	{
		static readonly ServiceIdentity Identity;

		static JeIdentity()
		{
			Identity = new ServiceIdentity("Connect3Dp.Host", Version.Parse(VersionInfo.Version), "Executable and user-configurable host utilizing Connect3Dp Services", [

				Connect3Dp.JeIdentity.GetServiceIdentity().IntoModule(),
				Lib3Dp.JeIdentity.GetServiceIdentity().IntoModule()

			]);
		}

		public static ServiceIdentity GetServiceIdentity()
		{
			return Identity;
		}
	}
}
