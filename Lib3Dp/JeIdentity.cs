using JeIdentity;

namespace Lib3Dp
{
	public class JeIdentity : IJeIdentityProvider
	{
		private static readonly ServiceIdentity Identity;

		static JeIdentity()
		{
			Identity = new ServiceIdentity("Lib3Dp", Version.Parse(VersionInfo.Version), "Vendor-agnostic remote management framework for 3D Printing", []);
		}

		public static ServiceIdentity GetServiceIdentity()
		{
			return Identity;
		}
	}
}
