# Connect3Dp

A .NET library providing core services, extension methods, logging, and controllers for vendor-agnostic 3D Printer Remote Management. Designed to be consumed by other applications, not executed directly.

## Services

### `JeWebSocketServer`

A generic, extensible WebSocket server for real-time bidirectional communication with 3D printing clients. Generic over a client type `C`, allowing consumers to attach per-connection state such as subscriptions or identity via a custom client wrapper.

Incoming messages are dispatched by topic name to registered handlers, added via the `With*` extension methods. Handlers can be simple (no payload) or typed (deserialize a JSON payload). Supports broadcasting to all connected clients and integrates with ASP.NET Core's routing pipeline.

### `JsonFileBasedMachineConfigurationStore`

Persists machine configurations to the local file system as JSON. Suitable for lightweight deployments that do not require a database.

---

## Extension Methods

### `JeWebSocketExtensions`

**Service Registration**

| Method | Description |
|---|---|
| AddJeWebSocketService\<C>(IServiceCollection, Func\<string, WebSocket, C>) | Register a JeWebSocketServer with a custom client type. The factory receives the client ID and WebSocket. |
| AddJeWebSocketServiceWithConnect3DpClient(IServiceCollection) | Register a JeWebSocketServer using the built-in Connect3DpClient. |
| GetRequiredConnect3DpJeWebSocketServer(IServiceProvider) | Resolve the registered JeWebSocketServer\<Connect3DpClient> from DI. |

**Endpoint Mapping**

| Method | Description |
|---|---|
| MapJeWebSocketServer\<C>(IEndpointRouteBuilder, string) | Map the WebSocket server to a specified route. |

**Action Mappings**

These methods register named topic handlers onto a JeWebSocketServer. Topic name constants are provided by the Topics static class.

| Method | Description |
|---|---|
| WithConfigurationsAction(JeWebSocketServer, MachineConnectionCollection) | Respond with all machine configurations when requested by a client. |
| WithSubscribeToLogs(JeWebSocketServer, BufferedLoggerProvider) | Allow a client to subscribe to real-time log streaming. |
| WithUnsubscribeToLogs(JeWebSocketServer, BufferedLoggerProvider) | Remove a client from the real-time log stream. |
| WithLogHistoryRetrieval(JeWebSocketServer, BufferedLoggerProvider) | Allow a client to retrieve buffered log history. |
| WithSubscribeAndUnsubscribeAction(JeWebSocketServer, MachineConnectionCollection) | Register subscribe and unsubscribe handlers for machine state update pushes. |
| WithStateBroadcasts(JeWebSocketServer, MachineConnectionCollection) | Broadcast machine state changes to all subscribed clients. |
| WithMarkAsIdleAction(JeWebSocketServer, MachineConnectionCollection) | Allow a client to mark a machine as idle. |
| WithResumeMachine(JeWebSocketServer, MachineConnectionCollection) | Allow a client to resume a paused machine. |
| WithPauseMachine(JeWebSocketServer, MachineConnectionCollection) | Allow a client to pause a running machine. |
| WithStopMachine(JeWebSocketServer, MachineConnectionCollection) | Allow a client to stop a machine. |
| WithFindMatchingSpoolsMachineAction(JeWebSocketServer, MachineConnectionCollection) | Allow a client to find spools matching a machine's current material requirements. |
| WithMachineFileStoreTotalUsage(JeWebSocketServer, IMachineFileStore) | Respond with aggregate file store usage across all machines. |
| WithMachineFileStoreMachineUsage(JeWebSocketServer, MachineConnectionCollection, IMachineFileStore) | Respond with file store usage for a specific machine identified in the message payload. |

Topics is a static class of string constants for all WebSocket action and topic names. Using it on both the server and client avoids magic strings.

---

### Other Extension Methods

| Method | Description |
|---|---|
| AddFileSystemMachineFileStore(IServiceCollection, FileSystemMachineFileStoreOptions) | Register a file system backed IMachineFileStore. |
| AddJsonFileBasedMachineConfigurationStore(IServiceCollection, string) | Register JsonFileBasedMachineConfigurationStore with a path to the JSON file. |
| AddMachineConnectionCollection(IServiceCollection) | Register MachineConnectionCollection in DI. |
| MapAllConnect3DpWebSocketActions(IServiceProvider) | Convenience method that registers all Connect3Dp WebSocket action handlers at once. |

---

## Logging

| Class | Description |
|---|---|
| BufferedLoggerProvider | ILoggerProvider that buffers log entries in memory for real-time forwarding to WebSocket subscribers. Accepts a capacity argument to cap the buffer size. |
| ForwardingLogger | ILogger that forwards captured entries to active client subscriptions. |

---

## Controllers

| Class | Description |
|---|---|
| MachineFilesController | ASP.NET Core API controller for uploading, listing, and deleting machine files via IMachineFileStore. Discovered automatically by AddControllers(). |

---

## Quick Setup

```csharp
// 1. Configure logging with buffered provider for real-time WS log streaming
var bufferedLoggerProvider = new BufferedLoggerProvider(500);
builder.Services.AddSingleton(bufferedLoggerProvider);
builder.Logging.AddConsole();
builder.Logging.AddProvider(bufferedLoggerProvider);

// 2. Register core services
builder.Services
    .AddMachineConnectionCollection()
    .AddFileSystemMachineFileStore(new FileSystemMachineFileStoreOptions(path, verifyHashes))
    .AddJsonFileBasedMachineConfigurationStore(path)
    .AddJeWebSocketServiceWithConnect3DpClient();

builder.Services.AddControllers(); // picks up MachineFilesController

// 3. Configure middleware and endpoints
app.UseWebSockets();
app.MapControllers();
app.MapJeWebSocketServer<JeWebSocketClientForConnect3Dp>("/ws");

// 4. Register all WebSocket action handlers
app.Services.MapAllConnect3DpWebSocketActions();

// 5. Load machine configurations and connect
var mc = app.Services.GetRequiredService<MachineConnectionCollection>();
var mcStore = app.Services.GetRequiredService<IMachineConfigurationStore>();

var cfgs = await mcStore.LoadConfigurations();
await mc.LoadFromConfigurations(cfgs);

_ = mc.ConnectIfDisconnected(default);

app.Run();
```
