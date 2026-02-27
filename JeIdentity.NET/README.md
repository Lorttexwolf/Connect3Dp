# JeIdentity

JeIdentity provides a common structure for describing a service and its dependencies (modules). Each service declares its name, version, description, and which modules it depends on — forming a clear, serializable identity that can be exposed or logged at runtime.

## Defining an Identity

Implement `IJeIdentityProvider` in your project and declare a static `ServiceIdentity`. Dependencies from other assemblies are pulled in via their own `JeIdentity.GetServiceIdentity()` and flattened as modules using `.IntoModule()`.
```cs
namespace Connect3Dp.Host
{
    public class JeIdentity : IJeIdentityProvider
    {
        static readonly ServiceIdentity Identity;

        static JeIdentity()
        {
            Identity = new ServiceIdentity(
                "Connect3Dp.Host",
                Version.Parse(VersionInfo.Version),
                "Executable and user-configurable host utilizing Connect3Dp Services",
                [
                    Connect3Dp.JeIdentity.GetServiceIdentity().IntoModule(),
                    Lib3Dp.JeIdentity.GetServiceIdentity().IntoModule()
                ]
            );
        }

        public static ServiceIdentity GetServiceIdentity() => Identity;
    }
}
```

## Serialized Output

The identity serializes to JSON, making it easy to expose via an endpoint.
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
      "description": "Vendor-agnostic remote management framework for 3D Printing."
    }
  ]
}
```

## Notes

- `VersionInfo` is generated at build time by [VersionInfoGenerator](https://github.com/notpeelz/VersionInfoGenerator), a source generator by notpeelz.