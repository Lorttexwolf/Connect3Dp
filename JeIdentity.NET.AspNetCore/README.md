# JeIdentity.AspNet

ASP.NET utilities for [JeIdentity.NET](../JeIdentity.NET/README.md), providing extension methods for dependency injection and automatic route mapping.

## Setup

Register the identity of the executing assembly as a singleton service:
```cs
builder.Services.AddJeIdentity();
```

## Exposing the Endpoint

Map the identity to an HTTP route using reflection via `JeIdentityResolver`:
```cs
app.MapJeIdentity("/jeIdentity");
```

A `GET` request to `/jeIdentity` will return the service identity as JSON:
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