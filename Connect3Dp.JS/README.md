# connect3dp

A TypeScript WebSocket client for Connect3Dp. Works in browsers and React Native. Uses the platform's native `WebSocket` global with no Node.js dependencies.

Data from the server is passed through as-is. The library does not merge, cache, or transform state. What the server sends is what you get.

---

## Installation

This package is not published to npm. Copy or symlink the `Connect3Dp.JS` directory into your project, then reference it directly:

```json
{
	"dependencies": {
		"connect3dp": "file:../Connect3Dp.JS"
	}
}
```

Run `npm install` after adding the reference. Make sure the library has been built first (`npm run build` inside `Connect3Dp.JS`).

**TypeGen prerequisite (only needed to regenerate types after C# model changes):**

```bash
dotnet tool install --global TypeGen
```

---

## Quick start

```typescript
import { Connect3DpClient, StateDetails } from "connect3dp";

const client = new Connect3DpClient("ws://localhost:5000/ws");

await client.connect();

const result = await client.subscribe("my-machine-id", StateDetails.Full);

if (result.isSuccess && result.fullState) {
	console.log("Status:", result.fullState.status);
	console.log("Nickname:", result.fullState.nickname);
	console.log("Nozzle temp:", result.fullState.extruders[0]?.tempC);
	console.log("Bed temp:", result.fullState.heatingElements["bed"]?.tempC);
}

client.onMachineState("my-machine-id", (id, data) => {
	if (data.fullChanges?.statusHasChanged) {
		console.log("Status changed to:", data.fullChanges.statusNew);
	}
});
```

---

## Connecting and reconnection

```typescript
const client = new Connect3DpClient("ws://localhost:5000/ws", {
	requestTimeoutMs: 10000,       // timeout for subscribe/query requests
	controlRequestTimeoutMs: 35000, // timeout for pause/resume/stop/markAsIdle
	reconnect: {
		enabled: true,
		initialDelayMs: 1000,        // wait 1s before first retry
		maxDelayMs: 30000,           // cap backoff at 30s
		maxAttempts: Infinity,       // retry forever
	},
});
```

| Option | Default | Description |
|---|---|---|
| `requestTimeoutMs` | 10000 | Timeout for subscribe and query actions |
| `controlRequestTimeoutMs` | 35000 | Timeout for machine control commands. Print start can take up to 30 seconds. |
| `reconnect.enabled` | true | Automatically reconnect on unexpected disconnect |
| `reconnect.initialDelayMs` | 1000 | First reconnect attempt delay |
| `reconnect.maxDelayMs` | 30000 | Maximum delay after exponential backoff |
| `reconnect.maxAttempts` | Infinity | Stop retrying after this many attempts |

When reconnected, the library re-subscribes to all machines that were active at the time of disconnect. You do not need to call `subscribe()` again.

---

## Machine subscriptions

Two detail levels are available:

**`StateDetails.Full`**: initial response includes the complete `IMachineState`. Subsequent broadcasts contain the full `MachineStateChanges` diff.

**`StateDetails.AtAGlance`**: initial response includes a lightweight `AtAGlanceMachineState` (status, capabilities, nickname, current job summary). Subsequent broadcasts contain only `AtAGlanceMachineStateChanges`.

```typescript
// Full detail
const full = await client.subscribe("printer-1", StateDetails.Full);
if (full.isSuccess && full.fullState) {
	const state = full.fullState;
	// Temperatures
	const nozzleTemp = state.extruders[0]?.tempC;
	const bedTemp = state.heatingElements["bed"]?.tempC;
	const chamberTemp = state.heatingElements["chamber"]?.tempC;
	// Filaments
	const ams = Object.values(state.materialUnits);
	// Lights and airduct
	const lights = state.lights;
	const airDuct = state.airDuctMode; // "None" | "Cooling" | "Heating"
}

// AtAGlance detail
const glance = await client.subscribe("printer-2", StateDetails.AtAGlance);
if (glance.isSuccess && glance.atAGlanceState) {
	console.log(glance.atAGlanceState.status); // "Idle", "Printing", etc.
}
```

---

## Listening to state updates

Broadcasts arrive as a `BroadcastedMachineStateUpdateData` object. One of `fullChanges` or `atAGlanceChanges` will be populated depending on your subscription level. The other will be null.

These are raw server diffs. See [Change type reference](#change-type-reference) below.

```typescript
client.onMachineState("printer-1", (machineId, data) => {
	const ch = data.fullChanges;
	if (!ch || !ch.hasChanged) return;

	// Scalar fields
	if (ch.statusHasChanged) {
		console.log(`Status: ${ch.statusPrevious} -> ${ch.statusNew}`);
	}

	// Temperature changes (extruder dict updates)
	for (const update of ch.extrudersUpdated) {
		if (update.value.tempCHasChanged) {
			console.log(`Extruder ${update.key} temp: ${update.value.tempCNew}°C`);
		}
	}

	// Bed/chamber heating element updates (full value replacement)
	for (const update of ch.heatingElementsUpdated) {
		console.log(`${update.key}: ${update.value.tempC}°C (target ${update.value.targetTempC}°C)`);
	}

	// Current job progress
	const job = ch.currentJobChanges;
	if (job?.percentageCompleteHasChanged) {
		console.log(`Progress: ${job.percentageCompleteNew}%`);
	}

	// Filament/AMS changes
	for (const added of ch.materialUnitsAdded) {
		console.log(`AMS added: ${added.key}`, added.value);
	}
	for (const update of ch.materialUnitsUpdated) {
		for (const trayUpdate of update.value.traysUpdated) {
			console.log(`AMS ${update.key} tray ${trayUpdate.key} filament changed`);
		}
	}

	// Notifications
	for (const notif of ch.notificationsAdded) {
		console.log(`[${notif.value.message.severity}] ${notif.value.message.title}`);
	}
});
```

### Change type reference

**Scalar field** (e.g. `status`, `nickname`):

```typescript
{ statusHasChanged: boolean, statusPrevious: "Idle" | null, statusNew: "Printing" | null }
```

**Dictionary field** (e.g. `extruders`, `materialUnits`):

```typescript
{
	extrudersAdded: Array<{ key: number, value: MachineExtruder }>,
	extrudersRemoved: number[],
	extrudersUpdated: Array<{ key: number, value: MachineExtruderChanges }>,
}
```

**Set field** (e.g. `jobHistory`, `localJobs`):

```typescript
{ jobHistoryAdded: HistoricPrintJob[], jobHistoryRemoved: HistoricPrintJob[] }
```

**Nested updater field** (e.g. `currentJob`):

```typescript
{ currentJobChanges: PrintJobChanges | null } // null = no job changes
```

---

## React state integration

Dictionary and set fields on the server send incremental diffs, not full replacements. The correct pattern is to seed your React state from the initial `subscribe()` result, then apply `added`, `removed`, and `updated` entries from each broadcast.

Here is an example using notifications, which is a `Record<string, Notification>` keyed by message ID:

```tsx
import { useState, useEffect } from "react";
import { Connect3DpClient, StateDetails } from "connect3dp";
import type { Notification } from "connect3dp";

function PrinterNotifications({ machineId }: { machineId: string }) {
	const [notifications, setNotifications] = useState<Record<string, Notification>>({});

	useEffect(() => {
		const client = new Connect3DpClient("ws://localhost:5000/ws");

		(async () => {
			await client.connect();
			const result = await client.subscribe(machineId, StateDetails.Full);
			if (result.isSuccess && result.fullState) {
				setNotifications(result.fullState.notifications);
			}
		})();

		const unsub = client.onMachineState(machineId, (_id, data) => {
			const ch = data.fullChanges;
			if (!ch) return;

			// Use the functional form so you always write against the latest state,
			// not a stale closure captured when the effect first ran.
			setNotifications((prev) => {
				const next = { ...prev };

				for (const key of ch.notificationsRemoved) {
					delete next[key];
				}
				for (const { key, value } of ch.notificationsAdded) {
					next[key] = value;
				}
				for (const { key, value } of ch.notificationsUpdated) {
					if (value.messageHasChanged && value.messageNew) {
						next[key] = { ...next[key], message: value.messageNew };
					}
					if (value.lastSeenAtHasChanged && value.lastSeenAtNew) {
						next[key] = { ...next[key], lastSeenAt: value.lastSeenAtNew };
					}
				}

				return next;
			});
		});

		return () => {
			unsub();
			client.disconnect();
		};
	}, [machineId]);

	return (
		<ul>
			{Object.values(notifications).map((n) => (
				<li key={n.message.id}>
					[{n.message.severity}] {n.message.title}
				</li>
			))}
		</ul>
	);
}
```

The same pattern applies to any dictionary field: `materialUnits`, `extruders`, `heatingElements`, `lights`, and so on. Set fields like `jobHistory` only have `Added` and `Removed` — there is no `Updated` for those.

---

## Key state fields

| Field | Path | Notes |
|---|---|---|
| Status | `state.status` | "Idle", "Printing", "Paused", etc. |
| Nickname / Brand / Model | `state.nickname`, `state.brand`, `state.model` | |
| Nozzle temperature | `state.extruders[0].tempC` | Per extruder number |
| Nozzle target temp | `state.extruders[0].targetTempC` | null when not heating |
| Nozzle diameter | `state.nozzles[0].diameter` | mm |
| Bed temperature | `state.heatingElements["bed"].tempC` | Key name is machine-defined |
| Chamber temperature | `state.heatingElements["chamber"].tempC` | Only if capability AirDuct present |
| Current job progress | `state.currentJob.percentageComplete` | 0–100 |
| Remaining time | `state.currentJob.remainingTime` | ISO 8601 duration |
| Lights | `state.lights` | `Record<string, boolean>` key is light name |
| Air duct mode | `state.airDuctMode` | "None", "Cooling", or "Heating" |
| AMS units | `state.materialUnits` | `Record<string, MUnit>` key is AMS unit ID |
| Filament in tray | `state.materialUnits["ams1"].trays[0].material` | |
| Local print files | `state.localJobs` | |
| Print history | `state.jobHistory` | |
| Notifications | `state.notifications` | `Record<string, Notification>` keyed by message ID |
| Capabilities | `state.capabilities` | Comma-separated flag string |

Check capabilities before accessing optional features:

```typescript
import { hasCapability, MachineCapabilityFlags } from "connect3dp";

if (hasCapability(state.capabilities, MachineCapabilityFlags.AirDuct)) {
	console.log("Air duct mode:", state.airDuctMode);
}
if (hasCapability(state.capabilities, MachineCapabilityFlags.Lighting)) {
	console.log("Lights:", state.lights);
}
```

---

## Machine controls

```typescript
await client.pause("printer-1");
await client.resume("printer-1");
await client.stop("printer-1");
await client.markAsIdle("printer-1");
```

All control methods use a 35-second timeout because print commands can take up to 30 seconds before the machine responds. If `result.isSuccess` is false, check `result.failureMessage` for a structured reason.

```typescript
const result = await client.stop("printer-1");
if (!result.isSuccess && result.failureMessage) {
	console.error(result.failureMessage.title, result.failureMessage.body);
}
```

---

## Log streaming

```typescript
// Subscribes and batches logs every 2 seconds
await client.subscribeLogs(2000);

// Listen for batches
const unsub = client.onLogs((entries) => {
	for (const entry of entries) {
		console.log(`[${entry.Level}] ${entry.Category}: ${entry.Message}`);
	}
});

// Retrieve historical logs
const history = await client.getLogHistory({
	Max: 100,
	MinLevel: "Warning",
	After: new Date(Date.now() - 60_000).toISOString(),
});

// Unsubscribe when done
await client.unsubscribeLogs();
unsub();
```

---

## File store

```typescript
const total = await client.getTotalFileStoreUsage();
if (total.isSuccess && total.usage) {
	console.log(`${total.usage.totalBytes} bytes across ${total.usage.fileCount} files`);
}

const machineUsage = await client.getMachineFileStoreUsage("printer-1");
```

---

## Events reference

All event handlers return an unsubscribe function.

```typescript
const off = client.on("reconnecting", (attempt, delayMs) => {
	console.log(`Reconnecting (attempt ${attempt}, waiting ${delayMs}ms)...`);
});
off(); // stop listening
```

| Event | Arguments | When it fires |
|---|---|---|
| `connected` | none | WebSocket opened |
| `disconnected` | `reason: string` | WebSocket closed (intentional or not) |
| `reconnecting` | `attempt: number, delayMs: number` | About to wait before a reconnect attempt |
| `reconnected` | none | Reconnected and all subscriptions restored |
| `error` | `error: Error` | WebSocket error or message parse failure |
| `message` | `msg: MessageToClient<unknown>` | Every raw inbound message before dispatch |

---

## Regenerating TypeScript types

Run this when C# state models change:

1. Build the C# projects: `dotnet build ../Lib3Dp ../Connect3Dp`
2. Regenerate types: `npm run generate-types` (inside `Connect3Dp.JS`)
3. Rebuild the library: `npm run build`

TypeGen reads the compiled DLLs and writes TypeScript interfaces into `src/generated/`. Requires the `TypeGen` dotnet tool installed globally:

```bash
dotnet tool install --global TypeGen
```

The `src/types/` directory contains hand-written types for the WebSocket protocol envelope and action payloads. These are not regenerated.
