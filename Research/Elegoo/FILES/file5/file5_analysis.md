# file5.js - Analysis Documentation

> **Chunk ID:** `590.5cc05721deff5431799a.js`  
> **Bundle Type:** Lazy-loaded Angular module (Printer Control Interface)  
> **Total Lines:** 14,446  
> **Parent Application:** cbdsa-mainboard-cmp (ELEGOO Centauri Black Web Interface)

---

## Table of Contents

1. [Overview](#overview)
2. [Module Structure](#module-structure)
3. [Printer Control Commands](#printer-control-commands)
4. [UI Components](#ui-components)
5. [Services](#services)
6. [Data Structures](#data-structures)
7. [WebRTC Video Streaming](#webrtc-video-streaming)
8. [Command Payloads](#command-payloads)
9. [Status Codes](#status-codes)
10. [Routing](#routing)

---

## Overview

This file is the **main printer control module** containing all the UI components and logic for controlling the ELEGOO Centauri Black printer. It implements:

- Print job start/pause/resume/stop
- Fan speed control
- Light control (LED + RGB)
- Axis homing
- File management
- Video streaming (WebRTC)
- Temperature monitoring
- Print history

### Key Importance

**This is the most critical file for understanding the printer control protocol.** It contains the actual implementations that send WebSocket commands to the printer.

---

## Module Structure

### Webpack Module IDs

| Module ID | Export | Description |
|-----------|--------|-------------|
| `6985` | MD5 hash library | File integrity verification |
| `7590` | `NetworkModule` | Main control module |
| `543` | Command enum (imported) | `r.E.COMMAND_NAME` |
| `5025` | Message builder (imported) | `s.K.getMsgBodyString()` |
| `8161` | PrinterService (imported) | Printer state management |
| `6557` | WebSocketService (imported) | WebSocket connection |

### Angular Components

| Component | Selector | Purpose |
|-----------|----------|---------|
| `ControlComponent` | `app-control` | Main control dashboard |
| `MonitorComponent` | `app-monitor` | Temperature monitoring |
| `LocalFilesComponent` | `app-local-files` | File browser |
| `PrintHistoryComponent` | `app-print-history` | Print history viewer |
| `PrinterTaskComponent` | `app-printer-task` | Print start dialog |
| `FanModalComponent` | `app-fan-modal` | Fan & light controls |
| `EditNameComponent` | `app-edit-name` | Rename files/printer |
| `ConfirmComponent` | `app-confirm` | Confirmation dialogs |

---

## Printer Control Commands

### Command Reference (From Code Analysis)

| Command Constant | ID | Method | Payload |
|------------------|-----|--------|---------|
| `SEND_PRINTER_START_PRINT` | 128 | `sendStartPrint()` | Filename, StartLayer, options |
| `SEND_PRINTER_SUSPEND_PRINT` | 129 | `stop()` | (none) |
| `SEND_PRINTER_STOP_PRINT` | 130 | `cease()` | (none) |
| `SEND_PRINTER_RESTORE_PRINT` | 131 | `stop()` (when paused) | (none) |
| `SEND_PRINTER_EDIT_NAME` | 192 | `changeName()` | Name |
| `EDIT_PRINTER_FILE_NAME` | 257 | `changeName()` | SrcPath, TargetPath |
| `GET_PRINTER_FILE_LIST` | 258 | (after rename) | Url |
| `EDIT_PRINTER_VIDEO_STREAMING` | 386 | `startVideo()`/`stopVideo()` | Enable: 0\|1 |
| `EDIT_PRINTER_AXIS_ZERO` | 402 | `changeZore()` | Axis |
| `EDIT_PRINTER_STATUS_DATA` | 403 | Multiple | Various |

---

## Command Payloads (Extracted from Code)

### Start Print (Cmd: 128)

**Location:** Lines 921-930

```javascript
{
    Filename: "/local/model.ctb",           // File path
    StartLayer: 0,                          // Starting layer (0 = beginning)
    Calibration_switch: 0 | 1,              // Hotbed calibration (0=off, 1=on)
    PrintPlatformType: 0,                   // Platform type
    Tlp_Switch: 0 | 1                       // Time-lapse recording (0=off, 1=on)
}
```

**UI Controls:**
- `controller.hotBedCheck` → `Calibration_switch`
- `controller.videoCheck` → `Tlp_Switch`
- `controller.PrintPlatformType` → `PrintPlatformType`

---

### Pause Print (Cmd: 129)

**Location:** Line 12650

```javascript
// No payload required
{}
```

---

### Stop Print (Cmd: 130)

**Location:** Line 12665

```javascript
// No payload required
{}
```

---

### Resume Print (Cmd: 131)

**Location:** Line 12650

```javascript
// No payload required
{}
```

**Logic:** Sent when `PrintInfo.Status === 6` (Suspended)

---

### Change Fan Speed (Cmd: 403)

**Location:** Lines 3940-3957

```javascript
{
    TargetFanSpeed: {
        ModelFan: 0-100,      // Model cooling fan
        AuxiliaryFan: 0-100,  // Auxiliary fan (min 50 when on)
        BoxFan: 0-100         // Chamber fan
    }
}
```

**Fan Toggle Defaults:**
| Fan | Off | On |
|-----|-----|-----|
| ModelFan | 0 | 10 |
| AuxiliaryFan | 0 | 80 |
| BoxFan | 0 | 100 |

**Increment/Decrement:** ±10% per click, capped at 0-100%

---

### Change Light Status (Cmd: 403)

**Location:** Lines 3963-3966

```javascript
{
    LightStatus: {
        SecondLight: 0 | 1,           // LED strip toggle
        RgbLight: [R, G, B]           // RGB values 0-255 each
    }
}
```

---

### Change Print Speed (Cmd: 403)

**Location:** Lines 12621-12624

```javascript
{
    PrintSpeedPct: 50-150           // Print speed percentage
}
```

---

### Home Axis (Cmd: 402)

**Location:** Lines 12673-12676

```javascript
{
    Axis: "Z"                       // Axis to home
}
```

**Guard:** Only sent if `CurrentStatus` does not include `1` (not printing)

---

### Enable Video Streaming (Cmd: 386)

**Location:** Lines 12728-12730

```javascript
{
    Enable: 1                       // 1 = start, 0 = stop
}
```

**Response includes:**
```javascript
{
    VideoUrl: "rtsp://..."          // Video stream URL
}
```

---

### Rename Printer (Cmd: 192)

**Location:** Lines 12785-12789

```javascript
{
    Name: "New Printer Name"
}
```

---

### Rename File (Cmd: 257)

**Location:** Lines 12794-12798

```javascript
{
    SrcPath: "/local/old_name.ctb",
    TargetPath: "/local/new_name.ctb"
}
```

---

## Status Codes

### Print Status Constants

**Location:** Lines 310-325

```javascript
class PrintStatus {
    static STATUS_LOADING = [0, 1, 15, 16, 18, 19, 20, 21];
    static STATUS_WAIT = [0];
    static STATUS_STOPED = [8, 14];
    static STATUS_STOPING = [1];
    static STATUS_COMPLETE = [9];
    static STATUS_SUSPENDING = [5];
    static STATUS_SUSPENDED = [6];
    static STATUS_PRINTING = [13];
    static STATUS_FILE_DETECTION = [10];
    static STATUS_RECOVERY = [12];
}
```

### Status Mapping

| Code | Constant | UI State |
|------|----------|----------|
| 0 | STATUS_WAIT | Idle |
| 1 | STATUS_STOPING | Stopping |
| 5 | STATUS_SUSPENDING | Pausing |
| 6 | STATUS_SUSPENDED | Paused |
| 7 | (resuming) | Resuming |
| 8 | STATUS_STOPED | Stopped |
| 9 | STATUS_COMPLETE | Completed |
| 10 | STATUS_FILE_DETECTION | Validating file |
| 12 | STATUS_RECOVERY | Power recovery |
| 13 | STATUS_PRINTING | Printing |
| 14 | STATUS_STOPED | Error stopped |

---

## UI Components

### ControlComponent (Main Dashboard)

**Selector:** `app-control`  
**Route:** `/network/control`

**Features:**
- Print status display
- Pause/Resume/Stop buttons
- Speed control slider
- Fan/Light modal triggers
- Video stream display
- File browser
- Print history

**Key Methods:**
| Method | Command | Description |
|--------|---------|-------------|
| `stop()` | 129 or 131 | Toggle pause/resume |
| `cease()` | 130 | Stop print |
| `changeSpeed(pct)` | 403 | Adjust print speed |
| `changeZore(axis)` | 402 | Home axis |
| `startVideo()` | 386 | Enable streaming |
| `stopVideo()` | 386 | Disable streaming |

---

### FanModalComponent

**Selector:** `app-fan-modal`

**Controls:**
- Model Fan slider (0-100%)
- Auxiliary Fan slider (0-100%)
- Box Fan toggle
- Light toggle (SecondLight)

**Methods:**
| Method | Description |
|--------|-------------|
| `add(fanType)` | Increase fan speed +10% |
| `reduce(fanType, min)` | Decrease fan speed -10% |
| `onChange(fanType, state)` | Toggle fan on/off |
| `changeLight()` | Toggle LED light |

---

### PrinterTaskComponent

**Selector:** `app-printer-task`

**Purpose:** Print confirmation dialog before starting a job.

**Options:**
- `hotBedCheck` → Calibration enabled
- `videoCheck` → Time-lapse recording
- `PrintPlatformType` → Platform selection

---

## Services

### WebRtcService

**Location:** Lines 620-750

Handles WebRTC video streaming from printer camera.

```javascript
class WebRtcService {
    // WebSocket for signaling
    socket: WebSocket;  // ws://${hostname}:8883
    
    // WebRTC connection
    pc: RTCPeerConnection;
    datachannel: RTCDataChannel;
    
    // Observable streams
    srcSubject: Subject<MediaStream>;
    iceConnectionStateSubject: Subject<string>;
    
    connectSocket(): void;
    playVideo(): void;
    init(): void;
}
```

**Signaling Protocol:**
```javascript
// Request offer
{ jsonrpc: "2.0", method: "offer", id: <random> }

// Send answer
{ jsonrpc: "2.0", method: "answer", params: <SDP>, id: <random> }
```

---

### FileUploadService

**Location:** Lines 560-600

Handles chunked file uploads to printer.

```javascript
class FileUploadService {
    bytesPerPiece = 1048576;  // 1MB chunks
    
    uploadFile(url, filename, file, md5, onSuccess, onError, onProgress): void;
    loopSend(file, offset, filename, ...): void;
}
```

**Upload FormData:**
```javascript
{
    TotalSize: string,      // Total file size
    Uuid: string,           // Upload session ID
    Offset: string,         // Current chunk offset
    Check: "1",             // Verify checksum
    "S-File-MD5": string,   // MD5 hash
    File: File              // Chunk data
}
```

---

### ConfirmService

**Location:** Lines 400-450

Dynamic confirmation dialog service.

```javascript
interface ConfirmMessage {
    title: string;
    body: string;
    btn: string;
    isCancelDisabled?: boolean;
}

confirm(message: ConfirmMessage): Promise<boolean>;
```

---

## WebRTC Video Streaming

### Connection Flow

```
1. UI toggles video → EDIT_PRINTER_VIDEO_STREAMING (Enable: 1)
2. Printer responds with VideoUrl
3. Check if VIDEO_WEBRTC capability exists
4. Connect to ws://<printer-ip>:8883 (signaling)
5. Send offer request
6. Receive SDP offer from printer
7. Create RTCPeerConnection
8. Set remote description (printer's offer)
9. Create answer
10. Set local description
11. Send answer via signaling
12. ICE candidates exchange
13. Stream connected → video plays
```

### Signaling Server

| Property | Value |
|----------|-------|
| Protocol | `ws://` |
| Port | `8883` |
| Messages | JSON-RPC 2.0 |

---

## Data Structures

### Printer Detail Structure

```typescript
interface PrinterDetail {
    Id: string;
    Data: {
        MainboardID: string;
        Name: string;
        VideoUrl: string;
        Capabilities: string[];  // ["VIDEO_WEBRTC", "TIMELAPSE"]
    };
    Status: {
        CurrentStatus: number[];
        PrintInfo: {
            Status: number;
            CurrentLayer: number;
            TotalLayer: number;
            Filename: string;
            TaskId: string;
            PrintSpeedPct: number;
            Progress: number;
        };
        CurrentFanSpeed: {
            ModelFan: number;
            AuxiliaryFan: number;
            BoxFan: number;
        };
        LightStatus: {
            SecondLight: number;
            RgbLight: [number, number, number];
        };
        TempOfHotbed: number;
        TempOfNozzle: number;
        TempOfBox: number;
        PlatFormType: number;
    };
    FileList: FileItem[];
    historyList: HistoryItem[];
    videoList: VideoItem[];
    socketResponseSubject: Subject<Response>;
}
```

---

## Routing

**Location:** Lines 14430-14440

```javascript
RouterModule.forChild([
    { path: "control",       component: ControlComponent },
    { path: "monitor",       component: MonitorComponent },
    { path: "local-files",   component: LocalFilesComponent },
    { path: "print-history", component: PrintHistoryComponent }
])
```

### Full URL Paths

| Path | Component | Description |
|------|-----------|-------------|
| `/network/control` | ControlComponent | Main control dashboard |
| `/network/monitor` | MonitorComponent | Temperature charts |
| `/network/local-files` | LocalFilesComponent | File browser |
| `/network/print-history` | PrintHistoryComponent | Job history |

---

## MD5 Hashing

**Location:** Lines 10-290 (Module 6985)

Pure JavaScript MD5 implementation used for file upload verification.

```javascript
// Usage
const md5 = new MD5();
md5.append(data);
const hash = md5.end();

// Or static
const hash = MD5.hash("data");

// ArrayBuffer support
const hash = MD5.ArrayBuffer.hash(arrayBuffer);
```

---

## Translation Keys

Key translation strings used in the control interface:

| Key | Context |
|-----|---------|
| `networkDeviceManager.control.status.stopSuccess` | Pause success |
| `networkDeviceManager.control.status.stopFail` | Pause failed |
| `networkDeviceManager.control.status.continueSuccess` | Resume success |
| `networkDeviceManager.control.status.continueFail` | Resume failed |
| `networkDeviceManager.control.status.ceaseSuccess` | Stop success |
| `networkDeviceManager.control.status.ceaseFail` | Stop failed |
| `networkDeviceManager.control.zoreSuccess` | Home success |
| `networkDeviceManager.control.zoreFail` | Home failed |
| `networkDeviceManager.control.axisSuccess` | Settings saved |
| `networkDeviceManager.control.axisFail` | Settings failed |
| `networkDeviceManager.error.webLinkError` | WebRTC error |

---

## Confidence Levels

| Item | Confidence | Notes |
|------|------------|-------|
| Command payloads | ✅ **Confirmed** | Directly from `getMsgBodyString()` calls |
| Status codes | ✅ **Confirmed** | Static class constants |
| Fan control logic | ✅ **Confirmed** | Complete implementation visible |
| Light structure | ✅ **Confirmed** | `SecondLight` + `RgbLight` array |
| WebRTC signaling | ✅ **Confirmed** | Full implementation |
| File upload protocol | ✅ **Confirmed** | Chunked upload visible |
| Routes | ✅ **Confirmed** | RouterModule config |
| Video streaming port | ✅ **Confirmed** | Port 8883 for WebRTC signaling |

---

## Quick Reference: Python Implementation

### Fan Control
```python
ws.send(json.dumps({
    "Id": "",
    "Data": {
        "Cmd": 403,
        "Data": {
            "TargetFanSpeed": {
                "ModelFan": 50,
                "AuxiliaryFan": 80,
                "BoxFan": 100
            }
        },
        "RequestID": uuid.uuid4().hex,
        "MainboardID": "<from-attributes>",
        "TimeStamp": int(time.time() * 1000),
        "From": 1
    }
}))
```

### Start Print
```python
ws.send(json.dumps({
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
        "RequestID": uuid.uuid4().hex,
        "MainboardID": "<from-attributes>",
        "TimeStamp": int(time.time() * 1000),
        "From": 1
    }
}))
```

### Pause/Resume
```python
# Pause (when printing)
ws.send(json.dumps({
    "Id": "", "Data": { "Cmd": 129, "Data": {}, ... }
}))

# Resume (when paused, Status=6)
ws.send(json.dumps({
    "Id": "", "Data": { "Cmd": 131, "Data": {}, ... }
}))
```

---

*Document generated from reverse engineering analysis of ELEGOO cbdsa-mainboard-cmp v1.0*
