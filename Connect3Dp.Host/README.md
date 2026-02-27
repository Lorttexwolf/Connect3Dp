# Connect3Dp.Host

Connect3Dp.Host is the ASP.NET Core host application for Connect3Dp. It wires up all services and exposes machine management over HTTP and WebSocket.

## Configuration

Create a `connect3dp.config.json` file in the same directory as the application. All fields are optional unless noted.

```json
{
  "Connect3Dp": {
    "MachineFileStore": {
      "Type": "FileSystem",
      "FileSystem": {
        "PathToDirectory": "./files",
        "VerifyHashes": false
      }
    },
    "MachineConfigurationStore": {
      "Type": "Json",
      "Json": {
        "Path": "./machineConfigurations.json"
      }
    }
  }
}

```

### Machine File Store

`Connect3Dp.MachineFileStore.Type` sets the storage backend for machine files. Defaults to `FileSystem`. Currently only `FileSystem` is supported.

`...FileSystem.PathToDirectory` is the directory where machine files are stored. Defaults to `./`.

`...FileSystem.VerifyHashes` controls whether file integrity is verified via hashing on read. Defaults to `false`.

### Machine Configuration Store

`Connect3Dp.MachineConfigurationStore.Type` sets the storage backend for machine configurations. Defaults to `Json`. Currently only `Json` is supported.

`...Json.Path` is the path to the JSON file storing machine configurations. Required when using the `Json` type.

#### Machine Configurations

The JSON file at `Connect3Dp.MachineConfigurationStore.Json.Path` holds the list of machines Connect3Dp will manage. Each entry requires a machine ID and a discriminated configuration block identifying the printer type and its connection details.

The exact shape of each entry depends on the printer type as defined in Lib3Dp.