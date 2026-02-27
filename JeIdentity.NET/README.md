# JeIdentity

JeIdentity is a common structure to identify your services and their modules or requirements.

Example involving Connect3Dp.Host and it's modules: Connect3Dp and Lib3Dp.

```cs
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
```

```json
{
  "name": "Connect3Dp.Host",
  "version": "0.0.1",
  "description": "Executable and user-configurable host utilizing Connect3Dp Services",
  "modules": [
    {
      "name": "Connect3Dp",
      "version": "0.0.1",
      "description": "Composable HTTP and WebSocket controllers for Lib3Dp"
    },
    {
      "name": "Lib3Dp",
      "version": "0.0.1",
      "description": "Vendor-agnostic remote management framework for 3D Printing. "
    }
  ]
}
```

In case you are wondering, `VersionInfo` is a [Source Generator made by notpeelz](https://github.com/notpeelz/VersionInfoGenerator).