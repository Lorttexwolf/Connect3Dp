# ELEGOO Centauri Black - Communication Protocol Documentation

> **Reverse Engineering Analysis**  
> **Application:** ELEGOO Mainboard Control Panel (`cbdsa-mainboard-cmp`)  
> **Target Device:** ELEGOO Centauri Black (Resin 3D Printer)  
> **Protocol:** SDCP (Smart Device Communication Protocol) over WebSocket

---

## Table of Contents

1. [What is SDCP?](#what-is-sdcp)
2. [Transport Layer](#transport-layer)
3. [Message Format](#message-format)
4. [Command Reference](#command-reference)
5. [Incoming Messages (Printer → Client)](#incoming-messages)
6. [Component → Data Map](#component-data-map)
7. [Status Codes & Enums](#status-codes--enums)
8. [API Quick Reference](#api-quick-reference)
9. [Example Payloads](#example-payloads)
10. [Confidence Levels](#confidence-levels)

---

## What is SDCP?

The printer uses a protocol called **SDCP** which stands for **Smart Device Communication Protocol**. It's a proprietary protocol developed by **Chitu Systems** (the company behind ChituBox slicer software) for communication between 3D printers and control applications.

### How Does SDCP Work?

Think of SDCP as a **structured conversation format** between your computer and the printer. Instead of sending raw bytes or obscure commands, SDCP uses human-readable JSON messages wrapped in a consistent envelope.

#### The Basic Flow:

```
┌─────────────┐                    ┌─────────────┐
│   Client    │                    │   Printer   │
│ (Your App)  │                    │ (Firmware)  │
└──────┬──────┘                    └──────┬──────┘
       │                                  │
       │  1. Connect via WebSocket        │
       │─────────────────────────────────>│
       │                                  │
       │  2. Send Command (JSON)          │
       │  { Cmd: 403, Data: {...} }       │
       │─────────────────────────────────>│
       │                                  │
       │  3. Receive Response (JSON)      │
       │  { Cmd: 403, Data: {Ack: 0} }    │
       │<─────────────────────────────────│
       │                                  │
       │  4. Printer pushes status        │
       │  { Cmd: 0, Data: {Status:...} }  │
       │<─────────────────────────────────│
       │                                  │
```

#### Key Concepts:

1. **Command IDs (`Cmd`)** - Every action has a unique number. For example:
   - `0` = Status broadcast (printer → client)
   - `128` = Start print
   - `403` = Edit settings (fan, lights, speed)

2. **Request/Response** - You send a command, the printer acknowledges with `Ack: 0` (success) or an error code.

3. **Push Messages** - The printer continuously broadcasts its status (temperatures, print progress, etc.) without being asked - you just listen.

4. **MainboardID** - Every printer has a unique ID. Commands must include this ID so the printer knows the message is meant for it.

#### Why JSON over WebSocket?

- **WebSocket** = Keeps a persistent connection open (unlike HTTP which connects/disconnects for each request)
- **JSON** = Human-readable, easy to debug, no binary parsing needed

#### SDCP vs Other Protocols:

| Protocol | Used By | Format | Transport |
|----------|---------|--------|-----------|
| **SDCP** | ELEGOO, Chitu-based printers | JSON | WebSocket |
| **G-code** | Most FDM printers | Text commands | Serial/USB |
| **OctoPrint API** | OctoPrint servers | JSON | HTTP REST |
| **Moonraker** | Klipper printers | JSON-RPC | HTTP/WebSocket |

SDCP is specifically designed for **resin printers** and includes features like UV exposure control, Z-axis management, and tank-level monitoring that G-code doesn't natively support.

---

## How to Build Messages (Copy-Paste Template)

This is the **universal message structure** you need to send ANY command to the printer:

### Python Template (Ready to Use) (TODO: change to C#)

```python
import json
import uuid
import time
from websocket import create_connection

# ============================================
# CONFIGURATION - Change these values
# ============================================
PRINTER_IP = "192.168.1.100"        # Your printer's IP
MAINBOARD_ID = "your_mainboard_id"  # Get from first status message

# ============================================
# MESSAGE BUILDER FUNCTION
# ============================================
def build_message(cmd: int, data: dict = None) -> str:
    """
    Build an SDCP message for any command.
    
    Args:
        cmd: Command ID (e.g., 403 for settings, 128 for start print)
        data: Command-specific payload (optional, defaults to empty dict)
    
    Returns:
        JSON string ready to send via WebSocket
    """
    message = {
        "Id": "",
        "Data": {
            "Cmd": cmd,
            "Data": data or {},
            "RequestID": uuid.uuid4().hex,          # Random 32-char hex string
            "MainboardID": MAINBOARD_ID,
            "TimeStamp": int(time.time() * 1000),   # Unix timestamp in ms
            "From": 1                               # Always 1 for client
        }
    }
    return json.dumps(message)

# ============================================
# EXAMPLE: Connect and send a command
# ============================================
ws = create_connection(f"ws://{PRINTER_IP}:3030/websocket")

# Example: Turn on lights
msg = build_message(403, {
    "LightStatus": {
        "SecondLight": 1,
        "RgbLight": [255, 255, 255]
    }
})
ws.send(msg)
response = ws.recv()
print(json.loads(response))

ws.close()
```

### Message Structure Breakdown

```
┌─────────────────────────────────────────────────────────────┐
│                      OUTER ENVELOPE                         │
├─────────────────────────────────────────────────────────────┤
│  {                                                          │
│    "Id": "",              ← Always empty for local control  │
│    "Data": { ... }        ← The actual command goes here    │
│  }                                                          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      DATA OBJECT                            │
├─────────────────────────────────────────────────────────────┤
│  {                                                          │
│    "Cmd": 403,            ← Command ID (what to do)         │
│    "Data": { ... },       ← Command parameters (how to do)  │
│    "RequestID": "abc...", ← Unique ID to match response     │
│    "MainboardID": "xyz",  ← Printer's unique identifier     │
│    "TimeStamp": 123456,   ← Current time in milliseconds    │
│    "From": 1              ← Always 1 (means "from client")  │
│  }                                                          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    COMMAND DATA (varies)                    │
├─────────────────────────────────────────────────────────────┤
│  For Cmd 403 (Edit Settings):                               │
│  {                                                          │
│    "LightStatus": {...}     ← Change lights                 │
│    "TargetFanSpeed": {...}  ← Change fans                   │
│    "PrintSpeedPct": 100     ← Change print speed            │
│  }                                                          │
│                                                             │
│  For Cmd 128 (Start Print):                                 │
│  {                                                          │
│    "Filename": "/local/model.ctb",                          │
│    "StartLayer": 0                                          │
│  }                                                          │
│                                                             │
│  For Cmd 129/130/131 (Pause/Stop/Resume):                   │
│  { }  ← Empty, no parameters needed                         │
└─────────────────────────────────────────────────────────────┘
```

### How to Get Your MainboardID

When you first connect, the printer sends an `sdcp/attributes` message containing the MainboardID. Here's how to capture it:

```python
import json
from websocket import create_connection

ws = create_connection("ws://YOUR_PRINTER_IP:3030/websocket")

# Listen for the first few messages
for i in range(5):
    msg = json.loads(ws.recv())
    
    # Look for attributes message
    if msg.get("Topic") == "sdcp/attributes":
        mainboard_id = msg["Attributes"]["MainboardID"]
        print(f"Your MainboardID: {mainboard_id}")
        break
    
    # Or extract from status message
    if msg.get("Topic") == "sdcp/status":
        mainboard_id = msg["Status"]["MainboardID"]
        print(f"Your MainboardID: {mainboard_id}")
        break

ws.close()
```

### Quick Reference: Common Commands

| What You Want | Cmd | Data Payload |
|---------------|-----|--------------|
| Turn light ON | 403 | `{"LightStatus": {"SecondLight": 1, "RgbLight": [255,255,255]}}` |
| Turn light OFF | 403 | `{"LightStatus": {"SecondLight": 0, "RgbLight": [0,0,0]}}` |
| Set fans | 403 | `{"TargetFanSpeed": {"ModelFan": 50, "AuxiliaryFan": 80, "BoxFan": 100}}` |
| Set print speed | 403 | `{"PrintSpeedPct": 100}` |
| Home Z axis | 402 | `{"Axis": "Z"}` |
| Start print | 128 | `{"Filename": "/local/file.ctb", "StartLayer": 0, ...}` |
| Pause print | 129 | `{}` |
| Stop print | 130 | `{}` |
| Resume print | 131 | `{}` |
| Enable video | 386 | `{"Enable": 1}` |
| Disable video | 386 | `{"Enable": 0}` |
| Get file list | 258 | `{"Url": "/local"}` |
| Rename printer | 192 | `{"Name": "My Printer"}` |

---

## Transport Layer

### Primary: WebSocket Connection

| Property | Value |
|----------|-------|
| **Protocol** | `ws://` |
| **Host** | Printer's IP address (same as web UI) |
| **Port** | `3030` |
| **Path** | `/websocket` |
| **Full URL** | `ws://<printer-ip>:3030/websocket` |

**Connection Example:**
```javascript
const ws = new WebSocket("ws://<PRINTER IP ADDRESS>:3030/websocket");
ws.onopen = () => console.log("Connected");
ws.onmessage = (event) => console.log(JSON.parse(event.data));
```

### Heartbeat

| Property | Value |
|----------|-------|
| **Interval** | 30 seconds |
| **Payload** | `"ping"` (string literal) |

```javascript
setInterval(() => ws.send("ping"), 30000);
```

### Secondary: MQTT (Cloud Features)

| Property | Value |
|----------|-------|
| **Broker** | `mqtt.chituiot.com` |
| **Port** | `80` |
| **Path** | `/mqtt` |
| **Protocol** | `ws://` (WebSocket over MQTT) |
| **Protocol Version** | 4 |

> **Note:** MQTT is used for cloud features (WebRTC video streaming, remote access). Local control uses WebSocket directly.

---

## Message Format

### Outgoing Message Structure (Client → Printer)

```json
{
  "Id": "",
  "Data": {
    "Cmd": <command_id>,
    "Data": { <command_parameters> },
    "RequestID": "<uuid_without_dashes>",
    "MainboardID": "<mainboard_id_or_empty>",
    "TimeStamp": <unix_timestamp_ms>,
    "From": 1
  }
}
```

| Field | Type | Description |
|-------|------|-------------|
| `Id` | string | Device ID (often empty for local) |
| `Cmd` | number | Command identifier (see Command Reference) |
| `Data` | object | Command-specific parameters |
| `RequestID` | string | UUID v4 without dashes (32 hex chars) |
| `MainboardID` | string | Mainboard identifier (from attributes) |
| `TimeStamp` | number | Unix timestamp in milliseconds |
| `From` | number | Source identifier (always `1` for web UI) |

### Incoming Message Structure (Printer → Client)

Messages are routed by topic:

```json
{
  "Topic": "sdcp/<topic_type>",
  "Status": { ... },        // for sdcp/status
  "Attributes": { ... },    // for sdcp/attributes
  "Data": { ... }           // for sdcp/response
}
```

| Topic | Description |
|-------|-------------|
| `sdcp/status` | Real-time printer status updates |
| `sdcp/attributes` | Printer attributes/capabilities |
| `sdcp/response` | Command responses |
| `sdcp/error` | Error notifications |

---

## Command Reference

### Command ID Enum

| Command | ID (Dec) | ID (Hex) | Category | Risk Level |
|---------|----------|----------|----------|------------|
| `GET_PRINTER_STATUS` | 0 | 0x00 | Query | ✅ Safe |
| `GET_PRINTER_ATTR` | 1 | 0x01 | Query | ✅ Safe |
| `SEND_PRINTER_DISCONNECT` | 64 | 0x40 | Control | ⚠️ State-changing |
| `SEND_PRINTER_START_PRINT` | 128 | 0x80 | Control | ⚠️ State-changing |
| `SEND_PRINTER_SUSPEND_PRINT` | 129 | 0x81 | Control | ⚠️ State-changing |
| `SEND_PRINTER_STOP_PRINT` | 130 | 0x82 | Control | ⚠️ State-changing |
| `SEND_PRINTER_RESTORE_PRINT` | 131 | 0x83 | Control | ⚠️ State-changing |
| `GET_BLACKOUT_STATUS` | 134 | 0x86 | Query | ✅ Safe |
| `SEND_BLACKOUT_ACTION` | 135 | 0x87 | Control | ⚠️ State-changing |
| `SEND_PRINTER_EDIT_NAME` | 192 | 0xC0 | Config | ✅ Safe |
| `SEND_PRINTER_SEND_FILE_END` | 255 | 0xFF | File | ✅ Safe |
| `EDIT_PRINTER_FILE_NAME` | 257 | 0x101 | File | ✅ Safe |
| `GET_PRINTER_FILE_LIST` | 258 | 0x102 | Query | ✅ Safe |
| `DELETE_PRINTER_FILE_LIST` | 259 | 0x103 | File | ⚠️ State-changing |
| `GET_PRINTER_FILE_DETAIL` | 260 | 0x104 | Query | ✅ Safe |
| `GET_PRINTER_HISTORY_ID` | 320 | 0x140 | Query | ✅ Safe |
| `GET_PRINTER_TASK_DETAIL` | 321 | 0x141 | Query | ✅ Safe |
| `DELETE_PRINTER_HISTORY` | 322 | 0x142 | File | ⚠️ State-changing |
| `GET_PRINTER_HISTORY_VIDEO` | 323 | 0x143 | Query | ✅ Safe |
| `EDIT_PRINTER_VIDEO_STREAMING` | 386 | 0x182 | Control | ✅ Safe |
| `EDIT_PRINTER_TIME_LAPSE_STATUS` | 387 | 0x183 | Config | ✅ Safe |
| `EDIT_PRINTER_AXIS_NUMBER` | 401 | 0x191 | Control | 🔴 Dangerous (movement) |
| `EDIT_PRINTER_AXIS_ZERO` | 402 | 0x192 | Control | 🔴 Dangerous (homing) |
| `EDIT_PRINTER_STATUS_DATA` | 403 | 0x193 | Control | ⚠️ State-changing |

---

## Command Details

### GET_PRINTER_STATUS (0)
**Purpose:** Query current printer status  
**Trigger:** On connect, periodic polling  
**Transport:** WebSocket

```json
{
  "Id": "",
  "Data": {
    "Cmd": 0,
    "Data": {},
    "RequestID": "a1b2c3d4e5f6789012345678901234ab",
    "MainboardID": "",
    "TimeStamp": 1705756800000,
    "From": 1
  }
}
```

**Response (via `sdcp/status`):**
```json
{
  "Topic": "sdcp/status",
  "Status": {
    "CurrentStatus": [0],
    "PrintInfo": {
      "TaskId": "task-uuid",
      "Status": 0,
      "PrintSpeedPct": 100,
      "TotalLayer": 100,
      "CurrentLayer": 50,
      "Filename": "/local/model.ctb"
    },
    "CurrentFanSpeed": {
      "ModelFan": 0,
      "BoxFan": 100,
      "AuxiliaryFan": 80
    },
    "LightStatus": 1,
    "PlatFormType": 0
  }
}
```

---

### GET_PRINTER_ATTR (1)
**Purpose:** Query printer capabilities and configuration  
**Trigger:** On connect  
**Transport:** WebSocket

```json
{
  "Id": "",
  "Data": {
    "Cmd": 1,
    "Data": {},
    "RequestID": "...",
    "MainboardID": "",
    "TimeStamp": 1705756800000,
    "From": 1
  }
}
```

**Response (via `sdcp/attributes`):**
```json
{
  "Topic": "sdcp/attributes",
  "Attributes": {
    "MainboardID": "ELEGOO-XXXXXX",
    "Capabilities": ["VIDEO_WEBRTC", "TIMELAPSE"],
    "VideoUrl": "",
    "Name": "Centauri Black"
  }
}
```

---

### SEND_PRINTER_START_PRINT (128)
**Purpose:** Start printing a file  
**Trigger:** User clicks "Print"  
**Risk:** ⚠️ State-changing (starts print job)

```json
{
  "Id": "",
  "Data": {
    "Cmd": 128,
    "Data": {
      "Filename": "/local/model.ctb",
      "StartLayer": 0,
      "Calibration_switch": 0,
      "PrintPlatformType": 0,
      "Tlp_Switch": 0
    },
    "RequestID": "...",
    "MainboardID": "ELEGOO-XXXXXX",
    "TimeStamp": 1705756800000,
    "From": 1
  }
}
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `Filename` | string | Full path to file (e.g., `/local/model.ctb`) |
| `StartLayer` | number | Layer to start from (0 = beginning) |
| `Calibration_switch` | number | 0 = disabled, 1 = enabled |
| `PrintPlatformType` | number | Platform type (0 = default) |
| `Tlp_Switch` | number | Time-lapse: 0 = off, 1 = on |

**Response:**
```json
{
  "Topic": "sdcp/response",
  "Data": {
    "Cmd": 128,
    "Data": {
      "Ack": 0
    }
  }
}
```

| Ack | Meaning |
|-----|---------|
| 0 | Success |
| Non-zero | Error (see error codes) |

---

### SEND_PRINTER_SUSPEND_PRINT (129)
**Purpose:** Pause the current print  
**Trigger:** User clicks "Pause"  
**Risk:** ⚠️ State-changing

```json
{
  "Id": "",
  "Data": {
    "Cmd": 129,
    "Data": {},
    "RequestID": "...",
    "MainboardID": "ELEGOO-XXXXXX",
    "TimeStamp": 1705756800000,
    "From": 1
  }
}
```

---

### SEND_PRINTER_STOP_PRINT (130)
**Purpose:** Cancel/stop the current print  
**Trigger:** User clicks "Stop"  
**Risk:** ⚠️ State-changing (terminates print, may move platform)

```json
{
  "Id": "",
  "Data": {
    "Cmd": 130,
    "Data": {},
    "RequestID": "...",
    "MainboardID": "ELEGOO-XXXXXX",
    "TimeStamp": 1705756800000,
    "From": 1
  }
}
```

---

### SEND_PRINTER_RESTORE_PRINT (131)
**Purpose:** Resume a paused print  
**Trigger:** User clicks "Resume"  
**Risk:** ⚠️ State-changing

```json
{
  "Id": "",
  "Data": {
    "Cmd": 131,
    "Data": {},
    "RequestID": "...",
    "MainboardID": "ELEGOO-XXXXXX",
    "TimeStamp": 1705756800000,
    "From": 1
  }
}
```

---

### GET_PRINTER_FILE_LIST (258)
**Purpose:** List files on printer storage  
**Trigger:** UI file browser, after file upload  

```json
{
  "Id": "",
  "Data": {
    "Cmd": 258,
    "Data": {
      "Url": "/local"
    },
    "RequestID": "...",
    "MainboardID": "",
    "TimeStamp": 1705756800000,
    "From": 1
  }
}
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `Url` | string | Directory path (e.g., `/local`, `/usb`) |

**Response:**
```json
{
  "Topic": "sdcp/response",
  "Data": {
    "Cmd": 258,
    "Data": {
      "Ack": 0,
      "FileList": [
        {
          "name": "/local/model.ctb",
          "type": 1,
          "size": 12345678
        }
      ]
    }
  }
}
```

---

### GET_PRINTER_TASK_DETAIL (321)
**Purpose:** Get details of a specific print task  
**Trigger:** When viewing print history  

```json
{
  "Id": "",
  "Data": {
    "Cmd": 321,
    "Data": {
      "Id": ["task-uuid-1", "task-uuid-2"]
    },
    "RequestID": "...",
    "MainboardID": "ELEGOO-XXXXXX",
    "TimeStamp": 1705756800000,
    "From": 1
  }
}
```

**Response:**
```json
{
  "Data": {
    "Cmd": 321,
    "Data": {
      "Ack": 0,
      "HistoryDetailList": [
        {
          "TaskId": "task-uuid",
          "TaskName": "/local/model.ctb",
          "Status": 9,
          "TimeLapseVideoStatus": 1,
          "TimeLapseVideoUrl": "/local/video.mp4",
          "ErrorStatusReason": 0
        }
      ]
    }
  }
}
```

---

### EDIT_PRINTER_STATUS_DATA (403)
**Purpose:** Modify real-time printer parameters  
**Trigger:** User adjusts fan speed, lights, print speed  
**Risk:** ⚠️ State-changing

#### Change Fan Speed
```json
{
  "Id": "",
  "Data": {
    "Cmd": 403,
    "Data": {
      "TargetFanSpeed": {
        "ModelFan": 10,
        "BoxFan": 100,
        "AuxiliaryFan": 80
      }
    },
    "RequestID": "...",
    "MainboardID": "ELEGOO-XXXXXX",
    "TimeStamp": 1705756800000,
    "From": 1
  }
}
```

| Fan | Range | Description |
|-----|-------|-------------|
| `ModelFan` | 0-100 | Model cooling fan (min ~10%) |
| `BoxFan` | 0-100 | Enclosure/chamber fan |
| `AuxiliaryFan` | 0-100 | Auxiliary/exhaust fan |

#### Change Light Status
```json
{
  "Data": {
    "Cmd": 403,
    "Data": {
      "LightStatus": 1
    }
  }
}
```

| Value | State |
|-------|-------|
| 0 | Off |
| 1 | On |

#### Change Print Speed
```json
{
  "Data": {
    "Cmd": 403,
    "Data": {
      "PrintSpeedPct": 100
    }
  }
}
```

| Range | Description |
|-------|-------------|
| 50-150 | Print speed percentage |

---

### EDIT_PRINTER_AXIS_ZERO (402)
**Purpose:** Home a specific axis  
**Trigger:** User clicks home button  
**Risk:** 🔴 Dangerous (platform movement)

```json
{
  "Id": "",
  "Data": {
    "Cmd": 402,
    "Data": {
      "Axis": "Z"
    },
    "RequestID": "...",
    "MainboardID": "ELEGOO-XXXXXX",
    "TimeStamp": 1705756800000,
    "From": 1
  }
}
```

| Axis | Description |
|------|-------------|
| `Z` | Z-axis (build platform) |

---

### EDIT_PRINTER_VIDEO_STREAMING (386)
**Purpose:** Enable/disable video stream  
**Trigger:** User toggles camera  

```json
{
  "Id": "",
  "Data": {
    "Cmd": 386,
    "Data": {
      "Enable": 1
    },
    "RequestID": "...",
    "MainboardID": "",
    "TimeStamp": 1705756800000,
    "From": 1
  }
}
```

| Enable | Action |
|--------|--------|
| 0 | Stop streaming |
| 1 | Start streaming |

**Response includes `VideoUrl` for stream access.**

---

### SEND_BLACKOUT_ACTION (135)
**Purpose:** Handle power failure recovery  
**Trigger:** User confirms/cancels resume after power loss  

```json
{
  "Data": {
    "Cmd": 135,
    "Data": {
      "action": 1
    }
  }
}
```

| action | Result |
|--------|--------|
| 0 | Cancel (don't resume) |
| 1 | Resume print |

---

## Status Codes & Enums

### Print Status (`PrintInfo.Status`)

| Code | Status | Description |
|------|--------|-------------|
| 0 | Idle | No active print |
| 1 | Stopping | Print being stopped |
| 5 | Suspending | Print being paused |
| 6 | Suspended | Print paused |
| 7 | Resuming | Print resuming |
| 8 | Stopped | Print cancelled |
| 9 | Completed | Print finished |
| 10 | File Detection | Validating file |
| 12 | Recovery | Power failure recovery |
| 13 | Printing | Actively printing |
| 14 | Stopped (error) | Stopped due to error |

### Current Status Array (`CurrentStatus`)

| Code | State |
|------|-------|
| 0 | Idle |
| 1 | Printing |
| 8 | File transferring |
| 15-21 | Loading states |

### Response Ack Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Unknown error |
| 2 | Specific error (check `ErrorStatusReason`) |

### Error Status Reasons

| Code | Description |
|------|-------------|
| 45 | Print error requiring acknowledgment |

---

## Component → Data Map

```
┌─────────────────────────┐     SENDS      ┌─────────────────────────┐
│   UI Component          │ ───────────► │   Printer Command       │
├─────────────────────────┤               ├─────────────────────────┤
│ File Browser            │ ───────────► │ GET_PRINTER_FILE_LIST   │
│ Print Button            │ ───────────► │ SEND_PRINTER_START_PRINT│
│ Pause Button            │ ───────────► │ SEND_PRINTER_SUSPEND    │
│ Resume Button           │ ───────────► │ SEND_PRINTER_RESTORE    │
│ Stop Button             │ ───────────► │ SEND_PRINTER_STOP       │
│ Fan Slider              │ ───────────► │ EDIT_PRINTER_STATUS_DATA│
│ Speed Slider            │ ───────────► │ EDIT_PRINTER_STATUS_DATA│
│ Light Toggle            │ ───────────► │ EDIT_PRINTER_STATUS_DATA│
│ Home Z Button           │ ───────────► │ EDIT_PRINTER_AXIS_ZERO  │
│ Camera Toggle           │ ───────────► │ EDIT_PRINTER_VIDEO      │
└─────────────────────────┘               └─────────────────────────┘

┌─────────────────────────┐    RECEIVES    ┌─────────────────────────┐
│   UI State              │ ◄─────────── │   Printer Message       │
├─────────────────────────┤               ├─────────────────────────┤
│ Print Progress          │ ◄─────────── │ sdcp/status             │
│ Layer Count             │ ◄─────────── │ sdcp/status             │
│ Fan Speeds              │ ◄─────────── │ sdcp/status             │
│ Printer Capabilities    │ ◄─────────── │ sdcp/attributes         │
│ MainboardID             │ ◄─────────── │ sdcp/attributes         │
│ Command Results         │ ◄─────────── │ sdcp/response           │
│ Error Messages          │ ◄─────────── │ sdcp/error              │
└─────────────────────────┘               └─────────────────────────┘
```

---

## API Quick Reference

### Safe Read-Only Commands

```javascript
// Get printer status
ws.send(JSON.stringify({
  Id: "", Data: { Cmd: 0, Data: {}, RequestID: uuid(), MainboardID: "", TimeStamp: Date.now(), From: 1 }
}));

// Get printer attributes
ws.send(JSON.stringify({
  Id: "", Data: { Cmd: 1, Data: {}, RequestID: uuid(), MainboardID: "", TimeStamp: Date.now(), From: 1 }
}));

// List files
ws.send(JSON.stringify({
  Id: "", Data: { Cmd: 258, Data: { Url: "/local" }, RequestID: uuid(), MainboardID: "", TimeStamp: Date.now(), From: 1 }
}));

// Get print history IDs
ws.send(JSON.stringify({
  Id: "", Data: { Cmd: 320, Data: {}, RequestID: uuid(), MainboardID: "", TimeStamp: Date.now(), From: 1 }
}));
```

### State-Changing Commands

```javascript
// Start print
ws.send(JSON.stringify({
  Id: "", Data: { Cmd: 128, Data: { Filename: "/local/model.ctb", StartLayer: 0, Calibration_switch: 0, PrintPlatformType: 0, Tlp_Switch: 0 }, RequestID: uuid(), MainboardID: "MAINBOARD-ID", TimeStamp: Date.now(), From: 1 }
}));

// Pause print
ws.send(JSON.stringify({
  Id: "", Data: { Cmd: 129, Data: {}, RequestID: uuid(), MainboardID: "MAINBOARD-ID", TimeStamp: Date.now(), From: 1 }
}));

// Resume print
ws.send(JSON.stringify({
  Id: "", Data: { Cmd: 131, Data: {}, RequestID: uuid(), MainboardID: "MAINBOARD-ID", TimeStamp: Date.now(), From: 1 }
}));

// Stop print
ws.send(JSON.stringify({
  Id: "", Data: { Cmd: 130, Data: {}, RequestID: uuid(), MainboardID: "MAINBOARD-ID", TimeStamp: Date.now(), From: 1 }
}));

// Set fan speed
ws.send(JSON.stringify({
  Id: "", Data: { Cmd: 403, Data: { TargetFanSpeed: { ModelFan: 50, BoxFan: 100, AuxiliaryFan: 80 } }, RequestID: uuid(), MainboardID: "MAINBOARD-ID", TimeStamp: Date.now(), From: 1 }
}));

// Toggle light
ws.send(JSON.stringify({
  Id: "", Data: { Cmd: 403, Data: { LightStatus: 1 } }, RequestID: uuid(), MainboardID: "MAINBOARD-ID", TimeStamp: Date.now(), From: 1 }
}));
```

---

## Example Payloads

### Minimal Connection Sequence

```javascript
const uuid = () => crypto.randomUUID().replace(/-/g, '');

const ws = new WebSocket('ws://192.168.1.100:3030/websocket');

ws.onopen = () => {
  // 1. Get status
  ws.send(JSON.stringify({
    Id: "", Data: { Cmd: 0, Data: {}, RequestID: uuid(), MainboardID: "", TimeStamp: Date.now(), From: 1 }
  }));
  
  // 2. Get attributes
  ws.send(JSON.stringify({
    Id: "", Data: { Cmd: 1, Data: {}, RequestID: uuid(), MainboardID: "", TimeStamp: Date.now(), From: 1 }
  }));
  
  // 3. Get file list
  ws.send(JSON.stringify({
    Id: "", Data: { Cmd: 258, Data: { Url: "/local" }, RequestID: uuid(), MainboardID: "", TimeStamp: Date.now(), From: 1 }
  }));
};

ws.onmessage = (event) => {
  const msg = JSON.parse(event.data);
  
  if (msg.Topic?.includes('sdcp/status')) {
    console.log('Status:', msg.Status);
  } else if (msg.Topic?.includes('sdcp/attributes')) {
    console.log('Attributes:', msg.Attributes);
    // Save MainboardID for future commands
  } else if (msg.Topic?.includes('sdcp/response')) {
    console.log('Response:', msg.Data);
  }
};

// Keep alive
setInterval(() => ws.send('ping'), 30000);
```

---

## Confidence Levels

| Item | Confidence | Notes |
|------|------------|-------|
| WebSocket endpoint | ✅ **Confirmed** | Directly from source code |
| Command IDs | ✅ **Confirmed** | Enum extracted from code |
| Message format | ✅ **Confirmed** | getMsgBodyString function analyzed |
| Status codes | 🟡 **High** | Inferred from UI logic |
| Parameter names | ✅ **Confirmed** | From payload construction |
| Fan speed ranges | 🟡 **High** | Inferred from UI constraints |
| MQTT cloud config | ✅ **Confirmed** | Environment config extracted |
| Topic routing | ✅ **Confirmed** | Message handler analyzed |
| Video streaming | 🟡 **High** | WebRTC integration present |
| Error codes | 🟠 **Medium** | Partial mapping from translation keys |

---

## Implementation Notes

1. **MainboardID Required:** Most state-changing commands require `MainboardID` from attributes response
2. **UUID Format:** RequestID must be UUID v4 without dashes (32 hex characters)
3. **Timestamps:** Use `Date.now()` for `TimeStamp` field
4. **From Field:** Always set to `1` for web client
5. **Response Matching:** Match responses by `Cmd` field, not `RequestID`
6. **Status Polling:** The printer pushes status updates automatically via `sdcp/status`

---

## Security Considerations

- No authentication required for local WebSocket
- Network access = full printer control
- Consider firewall rules if printer is on untrusted network

---

*Document generated from reverse engineering analysis of ELEGOO cbdsa-mainboard-cmp v1.0*
