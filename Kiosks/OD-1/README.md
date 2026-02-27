# Connect3Dp OD-1 Kiosk

An ESP32-S3 LVGL display sketch that connects to a **Connect3Dp** server via WebSocket and shows live machine status using the `AtAGlance` subscription level.

## Hardware

**Display:** Rectangle Bar RGB TTL TFT Display – 3.2" 320×820 (landscape, 820 px wide × 320 px tall)

## File Overview

| File | Purpose |
|---|---|
| `OD-1.ino` | Main Arduino sketch – WiFi, LVGL init, main loop |
| `config.h` | All user-editable settings (WiFi, server, machine ID, display) |
| `machine_state.h` | `MachineState` struct, `MachineStatus` enum, parse helpers |
| `ws_handler.h` | WebSocket client, subscribe/action messages, JSON message parsing |
| `ui_manager.h` | LVGL v8 widget creation and state-driven refresh |

---

## UI Layout

```
┌──────────────────────────────────────────────────────────────────────────┐
│  [ STATUS LABEL         (colour-coded bar, full width, 50 px)         ]  │
│  Nickname / Machine ID                              [Mark as Idle]        │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │ Job: <name>                                                         │  │
│  │ ████████████████░░░░░░░░░░░░  45%                                  │  │
│  │ Remaining: 00:30:00 / 01:30:00                                      │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                           ⊕ Connected     │
└──────────────────────────────────────────────────────────────────────────┘
```

The **Mark as Idle** button appears on the right only when the machine status is **Printed** or **Canceled**. Tapping it sends a `machine/markAsIdle` action to the server. Buttons have a black background, white border, and white text.

---

## Quick Start

### 1. Install Libraries (Arduino Library Manager)

| Library | Version | Author |
|---|---|---|
| ArduinoWebsockets | latest | Gil Maimon |
| ArduinoJson | 6.x | Benoit Blanchon |
| lvgl | 8.x | LVGL LLC |

> **Note:** Wire the 3.2" 320×820 RGB TFT panel to your ESP32-S3 board and
> configure the LVGL display driver (flush callback, draw buffer, tick source)
> in `OD-1.ino` to match your hardware. No additional library is required beyond
> the ones listed above.

### 2. Edit `config.h`

```cpp
#define WIFI_SSID       "YourNetwork"
#define WIFI_PASSWORD   "YourPassword"
#define C3DP_HOST       "192.168.1.100"   // Connect3Dp server IP
#define C3DP_PORT       5000
#define C3DP_MACHINE_ID "your-machine-id" // must match the server config
```

`C3DP_MACHINE_ID` must match the `ID` field in the
`MachineConnectionConfiguration` used by the Connect3Dp server.

### 3. Configure LVGL (`lv_conf.h`)

Enable the fonts used by the UI (edit `lv_conf.h` inside your LVGL library folder):

```c
#define LV_FONT_MONTSERRAT_12  1
#define LV_FONT_MONTSERRAT_14  1
#define LV_FONT_MONTSERRAT_16  1
#define LV_FONT_MONTSERRAT_20  1
#define LV_FONT_MONTSERRAT_28  1
```

### 4. Flash & Run

Open the `OD-1` folder as an Arduino project, select your ESP32-S3 board, and upload.

---

## WebSocket Protocol (reference)

The sketch uses the `AtAGlance` subscription detail level.

**Subscribe (client → server)**
```json
{
  "Action": "machine/subscribe",
  "Data": { "MachineID": "<id>", "DetailOfState": "AtAGlance" }
}
```

**Initial state response** (`Topic = "machine/subscribe"`)
```json
{
  "Topic": "machine/subscribe",
  "Data": {
    "AtAGlanceState": {
      "Status": "Idle",
      "Capabilities": "Control, Lighting",
      "Nickname": "My Printer",
      "Job": null
    }
  }
}
```

**Delta update broadcast** (`Topic = "machine/<id>/state"`)
```json
{
  "Topic": "machine/<id>/state",
  "Data": {
    "AtAGlanceChanges": {
      "StatusHasChanged": true, "StatusNew": "Printing",
      "NicknameHasChanged": false,
      "CurrentJobChanges": {
        "NameHasChanged": true, "NameNew": "benchy.3mf",
        "PercentageCompleteHasChanged": true, "PercentageCompleteNew": 0,
        "RemainingTimeHasChanged": true, "RemainingTimeNew": "01:30:00",
        "TotalTimeHasChanged": true,     "TotalTimeNew": "01:30:00"
      }
    }
  }
}
```

`TimeSpan` values are serialised by .NET as `[-][d.]hh:mm:ss[.fffffff]`
(e.g. `"01:30:00"` or `"1.02:30:00"` for values over 24 hours).

**Mark as Idle (client → server)** — sent when the user taps the button
```json
{ "Action": "machine/markAsIdle", "Data": { "MachineID": "<id>" } }
```

---

## Authentication

If the Connect3Dp server is behind `JeIdentity` authentication, you will need
to pass credentials or a bearer token in the WebSocket upgrade request.
`ArduinoWebsockets` supports extra headers via
`client.addHeader("Authorization", "Bearer <token>")` before calling
`client.connect(...)`.
