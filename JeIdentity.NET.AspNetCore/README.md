# JeIdentity ASP.NET

An ease-of-use utility package for [JeIdentity.NET](../JeIdentity.NET/README.md) with useful extension methods for Dependency Injection and auto-routing.

```cs
builder.Services.AddJeIdentity();
```

Stores the JeIdentity of the executing assembly as a Singleton Service.

```cs
app.MapJeIdentity("/jeIdentity");
```

For convenience, the defined `JeIdentity` can mapped via Reflection using  `JeIdentityResolver` to the desired route displayed as JSON.

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