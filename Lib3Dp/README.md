# Lib3Dp Source

## Reverse Engineering Documents

[Bambu Lab](./Research/BambuLab/README.md)

[Creality K1C](./Research/Creality/CrealityK1C.md)

[ELEGOO Centauri Carbon / SDCP](./Research/Elegoo/README.md)

## Creating a Connector

A surface-level guide for creating a 3D printer connector by extending `MachineConnection`. It is not exhaustive refer to `BBLMachineConnection` as a concrete reference implementation throughout.

### What is a Connector?

A connector adapts Lib3Dp's abstract machine model to a real device or vendor protocol, translating abstract actions into concrete protocol calls (MQTT, HTTP, FTP, USB, serial, etc.) and updating runtime state via `CommitState(...)`.

### Getting Started

A connector is split into two halves: the base class handles the operation lifecycle coordination, timeouts, state diffing, scheduling and your implementation handles the protocol-specific work. You wire into the base class by overriding `*_Internal` methods, and it takes care of the rest.

Start by deriving from `MachineConnection` and passing the required arguments to the base constructor:

```csharp
public class MyMachineConnection : MachineConnection
{
    private readonly MyDeviceClient Client;

    public MyMachineConnection
        (IMachineFileStore fileStore, string nickname, string id, string brand, string model)
        : base(fileStore, nickname, id, brand, model)
    {
        Client = new MyDeviceClient(id);
    }
}
```

From there, implement the two required abstract members. `Connect_Internal` establishes the connection to the device and should commit state once connected. `DownloadLocalFile` fetches a file from the device by handle and writes it to a stream.

```csharp
protected override async Task Connect_Internal()
{
    await Client.ConnectAsync();

    // Declare what this device supports so the framework knows which operations to expose.
    CommitState(changes => changes
        .SetIsConnected(true)
        .SetCapabilities(MachineCapabilities.Control | MachineCapabilities.StartLocalJob | MachineCapabilities.LocalJobs));
}
```

Beyond those, override only the virtual operations your device supports. Most connectors implement `PrintLocal_Internal`, `Pause_Internal`, `Resume_Internal`, and `Stop_Internal` as the core print lifecycle. `ClearBed_Internal` is for printers that can automatically eject or clear the bed after a job completes useful in unattended or farm scenarios and `BeginMUHeating_Internal` / `EndMaterialUnitHeating_Internal` are for multi-material systems that support pre-heating independently of a running print.

The framework only exposes operations that are declared in `MachineCapabilities`, so your overrides and capability declarations need to stay in sync. Implementing `ClearBed_Internal` without declaring the corresponding capability means it will never be reachable.

### Performing Operations

The public methods on `MachineConnection` handle coordination for you each one invokes your corresponding `*_Internal` override and waits for the machine state to reflect the expected change. Your implementation just performs the protocol-specific work and commits any resulting state changes:

```csharp
protected override Task Pause_Internal()
{
    return this.MQTT.PublishPause();
}
```

If you need to perform synchronized work outside of an `*_Internal` method, the base `Mono` field is available for that purpose. Note: you usually do not need to call `Mono` directly the public methods call it internally and then invoke your `*_Internal` implementations.

#### Operation Results

Lib3Dp prefers not to throw exceptions for expected device conditions `MachineOperationResult` exists for this reason. Most public operations return `Task<MachineOperationResult>`, letting callers handle failure gracefully without try/catch. For precondition failures (e.g. `State.IfNotCapable(...)`), return a failed result immediately, paired with a relevant `MachineMessage` from your constants.

### State Updates and CommitState

Always update runtime state via `CommitState(Action<MachineStateUpdate>)`. `MachineConnection` will apply the changes, run scheduling, and invoke `OnChanges` callbacks.

```csharp
CommitState(changes => changes.SetIsConnected(true));
CommitState(changes => changes.SetCapabilities(machineFeatures));
```

When you commit scheduled prints or MU heating schedules, the scheduler starts tasks for those entries automatically and calls into the appropriate connector methods when the time arrives.

### File Handling

Implement `DownloadLocalFile(...)` to handle the handle types your connector exposes. The base `DownloadFile` helper checks `FileStore.Contains(handle)` and attempts a `Read` first, falling back to your `DownloadLocalFile` implementation. Write directly to the provided `destinationStream` where possible, and throw an `IOException` for unknown handle types so the base method can fail cleanly.

```csharp
protected override async Task DownloadLocalFile(MachineFileHandle fileHandle, Stream destinationStream)
{
    if (BBLFiles.TryParseAs3MFHandle(fileHandle, out var filePath))
    {
        await FTP.DownloadFile(filePath, destinationStream);
    }
    else if (BBLFiles.TryParseAs3MFThumbnailHandle(fileHandle, out filePath))
    {
        var (bbl3MF, _) = await FTP.Download3MF(filePath);
        if (bbl3MF.ThumbnailSmall == null) throw new IOException("Thumbnail does not exist");
        destinationStream.Write(bbl3MF.ThumbnailSmall, 0, bbl3MF.ThumbnailSmall.Length);
    }
    else
    {
        throw new IOException();
    }
}
```

#### File Handles and File Stores

`MachineFileHandle` is a value-type record with fields `MachineID`, `URI`, `MIME`, and `HashSHA256`. Equality is value-based keep `HashSHA256` stable across reads to ensure deduplication works correctly.

`IMachineFileStore` abstracts storage and retrieval of machine-local files, exposing methods to stream, store, read, check, and delete handles, as well as query storage usage. `FileSystemMachineFileStore` is the provided implementation, storing files on disk organised by machine ID and hash. Provide helper methods to create and parse handles for your connector's file types (e.g. `BBLFiles.HandleAs3MF(...)` and `BBLFiles.TryParseAs3MFHandle(...)`), and use the `MIME` field to describe content so consumers know how to interpret the stream.

### Notifications and Messages

Use `MachineMessage` and `MachineNotification` to surface issues to the interface. Notifications are appropriate for connection errors, device-reported warnings (thermal runaway, missing media, incompatible firmware), and scheduled print failures. Define reusable messages in a centralized constants file (e.g. `Constants/MachineMessages.cs`) rather than inline. Include enough body text that the user understands why the message exists and what to do avoid highly dynamic data in message fields as signatures are computed via `MachineMessage.ComputeSignature` for deduplication.

```csharp
var msg = new MachineMessage(
    "Authentication Failed",
    "Access token rejected. Please re-enter credentials. (Error 401: token expired)",
    MachineMessageSeverity.Error,
    MachineMessageActions.CheckConnection,
    new MachineMessageAutoResole(WhenConnected: true, WhenStatus: null, WhenPrinting: null)
);

CommitState(changes => changes.SetNotifications(new MachineNotification(msg)));
```

#### Auto-resolve and Manual-resolve

Messages support two lifecycle mechanisms:

**Auto-resolve** uses `MachineMessageAutoResole`, which carries three predicate fields (`WhenConnected`, `WhenStatus`, `WhenPrinting`). The framework evaluates these inside every `CommitState(...)` call and removes any notification whose conditions are satisfied so a message with `WhenConnected: true` will clear itself the next time the machine connects, with no additional code required. 

**Manual-resolve** uses `MachineMessageActions`, a flags enum that advertises which UI actions are relevant to a given message (Resume, Pause, Cancel, Refresh, CheckConnection, UnsupportedFirmware, ClearBed). These are metadata only the interface is responsible for mapping them to the corresponding `MachineConnection` calls. To remove a notification programmatically from connector code, use `RemoveNotifications` directly:

```csharp
CommitState(changes => changes.RemoveNotifications(existingNotification));
```

Note that message signatures are derived from content, changing the `Title` or `Body` text breaks deduplication. Keep the user-facing summary stable and push volatile debug details to logs instead.