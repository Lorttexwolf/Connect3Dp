# Creality Print 6.3 LAN

**Date Updated: 12/4/2025**

These notes summarize observed but not fully verified behavior of Creality Print 6.3’s LAN communication. This information is based on network inspection and partially de-minified JavaScript from the Creality Print dashboard. Many details are still unclear.

## Communication Overview

Creality Print 6.3 appears to communicate with printers over LAN using a [WebSocket](https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API) connection on port `9999`. Messages are JSON objects using a `{ method, params }` structure.

## Commands

### Stop

```JSON
{
    "method": "set",
    "params": { "stop": 1 }
}

```

### Pause

```JSON
{
    "method": "set",
    "params": { "pause": 1 }
}

```

### Resume

```JSON
{
    "method": "set",
    "params": { "pause": 0 }
}

```

### Light Control

```JSON
{
    "method": "set",
    "params": { "lightSw": 1 | 0 }
}

```

### Send GCode

```JSON
{
    "method": "set",
    "params": { "gcodeCmd": "..." }
}

```

### Start Print

Exact parameters are not confirmed.

```JSON
{
    "method": "set",
    "params": {
        "opGcodeFile": "printprt:...",
        "enableSelfTest": 0 | 1
    }
}
```

## Requesting Information

Sending "method": "get" with various properties prompts the printer to return specific data. Exact returns aren't documented yet.

```JSON
{
    "method": "get",
    "params": {
        "reqGcodeFile": 1,
        "reqGcodeList": 1,

        "reqHistory": 1,

        "boxsInfo": 1,
        "boxConfig": 1,

        "reqPrintObjects": 1,

        "reqElapseVideoList": 1,

        "cfsList": 1,
        "reqMaterials": 1,

        "pFileList": 1,
        "onePageNum": 10
    }
}
```

- Several fields relate to the [Creality Filament System (CFS)](https://store.creality.com/products/cfs-creality-filament-system).
- Property `reqPrintObjects` may reference [object-based exclusion](https://wiki.bambulab.com/en/general/skipping-objects) during printing.
- Properties `pFileList` and `onePageNum` may involved paginated directory listing of files?

## State Reference

### Device State

Unconfirmed, AI was utilized to extract potential states. All will be confirmed later into the research.

| Category | Field | Meaning |
|----------|--------|---------|
| **Printer State** | deviceState | Printing, Idle, etc, exacts UNKNOWN |
| **Print Job** | printState | UNKNOWN, a more detailed print stage? Calibrating or Bed-Leveling? |
| **Print Job** | printProgress | % Completion |
| **Print Job** | printLeftTime | UNIT UNKNOWN |
| **Print Job** | printJobTime | UNIT UNKNOWN |
| **Print Job** | layer | Current Layer |
| **Print Job** | TotalLayer | Total Layers |
| **Print Job** | printPath | Path of file being Printed |
| **Print Job** | opGcodeFile | Selected GCode File |
| **Print Job** | commonGcodeInfo | UNKNOWN, Metadata parsed from GCode? |
| **Print Job** | enableSelfTest | Whether pre-print Self-Test is Enabled |
| **Motion & Control** | lightSw | Whether the LEDs are Lit |
| **Temperatures** | toolTemp | Hotend Temperature, not nozzle? |
| **Temperatures** | bedTemp | Bed Temperature |
| **Temperatures** | chamberTemp | Chamber Temperature |
| **Temperatures** | nozzleTemp | Nozzle Temperature |
| **Material / CFS / Box System** | materialStatus | UNKNOWN |
| **Material / CFS / Box System** | materialState | UNKNOWN |
| **Material / CFS / Box System** | materialCode | UNKNOWN |
| **Material / CFS / Box System** | serbox | UNKNOWN |
| **Files & Storage** | localFiles | Files stored on the printer. |
| **Files & Storage** | repoFiles | UNKNOWN |
| **Files & Storage** | repoFileStatus | UNKNOWN |
| **Files & Storage** | fileInfo | UNKNOWN |
| **Files & Storage** | fileRoutes | UNKNOWN |
| **Files & Storage** | pFileList | UNKNOWN, Paginated file list? |
| **Errors** | error | Error Code |
| **Errors** | errorMsg | Human-friendly Error Message |
| **Errors** | rtmpIsOverload | UNKNOWN |
| **Misc** | webcam | UNKNOWN, Webcam Metadata? |
| **Misc** | camUrl | Camera URL |
| **Misc** | hardwareState | UNKNOWN |
| **Misc** | GcodeFilePath | UNKNOWN |
| **Misc** | firmwareVersion | Firmware Version |
| **Misc** | ven | UNKNOWN, Vendor or Device Info? |

<!-- | **Errors** | repoPlrStatus | Internal job/error state latch used by UI | -->
<!-- | **Misc** | slicerComment | Gcode header comment. | -->
<!-- | **Material / CFS / Box System** | boxsInfo | Filament box info. | -->
<!-- | **Material / CFS / Box System** | boxConfig | Filament box configuration. | -->
<!-- | **Material / CFS / Box System** | boxState | Filament box state. | -->
<!-- | **Material / CFS / Box System** | boxErrorCode | Box/CFS error code. | -->
<!-- | **Material / CFS / Box System** | cfsList | List of CFS modules. | -->
<!-- | **Material / CFS / Box System** | cfsMaterial | Material info for CFS. | -->
<!-- | **Material / CFS / Box System** | cfsFl | Filament length? (Observed but unconfirmed.) | -->
<!-- | **Material / CFS / Box System** | cfsOffset | Offset for spool switching. | -->
<!-- | **Material / CFS / Box System** | fpfl | Filament position/length (not fully verified) | -->
<!-- | **Temperatures** | printMaterialTemp | Temperature hints from material. | -->
<!-- | **Motion & Control** | feedRate | Feedrate multiplier. | -->
<!-- | **Motion & Control** | feedMax | Maximum feedrate. | -->
<!-- | **Motion & Control** | Xpos | X-axis position. | -->
<!-- | **Motion & Control** | Ypos | Y-axis position. | -->
<!-- | **Motion & Control** | Zpos | Z-axis position. | -->
<!-- | **Motion & Control** | Epos | Extruder position. | -->
<!-- | **Motion & Control** | axisOps | Combined axis operations. | -->
<!-- | **Motion & Control** | fan0Speed | Fan 0 speed. | -->
<!-- | **Motion & Control** | fan1Speed | Fan 1 speed. | -->

## Relevant Extracted Code

Possibly relevant code from the extracted JS source of the Creality Print device dashboard in [CeX73yR3.JS](./CeX73yR3.js). 

```JS
static StartNormalPrint_LAN(t, r, n = 0) {
    return this.execute(t, {
        print: `/media/mmcblk0p1/creality/gztemp/${r}`
    })
}
```

```JS
setDataFromDevice(t, dataFromPrinter) {
    var n, a, i, o, l;
    for (const s of Ut().data.printerList)
        for (const localState of s.list)
            if (((n = localState.socket) == null ? void 0 : n.ip) == (t == null ? void 0 : t.ip)) {
                if (localState.online = !0, Object.keys(dataFromPrinter).forEach(d => {
                    try {
                        localState.timeStamp = Date.now(), JSON.stringify(localState.data[d]) !== JSON.stringify(dataFromPrinter[d]) && (localState.data[d] = dataFromPrinter[d])
                    } catch (u) {
                        console.log(u)
                    }
                }), dataFromPrinter.hasOwnProperty("repoPlrStatus") && (localState.repoPlrStatus = dataFromPrinter.repoPlrStatus, localState.repoPlrStatus == 0 && localState.err.errcode == 115 ? (localState.err.errcode = 0, localState.err.key = 0) : (localState.err.errcode = dataFromPrinter.repoPlrStatus == 1 ? 115 : (a = localState.err) == null ? void 0 : a.errcode, localState.err.key = dataFromPrinter.repoPlrStatus == 1 ? 115 : (i = localState.err) == null ? void 0 : i.key)), dataFromPrinter.hasOwnProperty("err") && localState.repoPlrStatus !== 1 && (localState.err = dataFromPrinter.err), dataFromPrinter.hasOwnProperty("materialStatus") && (localState.materialStatus = dataFromPrinter.materialStatus, localState.err.errcode = dataFromPrinter.materialStatus == 1 ? 2839 : (o = localState.err) == null ? void 0 : o.errcode, localState.err.key = dataFromPrinter.materialStatus == 1 ? 2839 : (l = localState.err) == null ? void 0 : l.key, dataFromPrinter.materialStatus == 1 && (localState.materialTimerId && clearTimeout(localState.materialTimerId), localState.materialTimerId = setTimeout(() => {
                    this.reconnectWebSocket(localState)
                }, 1e4))), localState.err.errLevel = localState.err.errcode >= 1 && localState.err.errcode <= 100 || localState.err.errcode == 2e4 || localState.err.errcode == 20010 ? 1 : 0, localState.previewimg = "http://" + localState.address + ":80/downloads/original/current_print_image.png", dataFromPrinter.hasOwnProperty("printProgress") && (localState.printProgress = dataFromPrinter.printProgress), dataFromPrinter.hasOwnProperty("printLeftTime") && (localState.printLeftTime = Tl(dataFromPrinter.printLeftTime), localState.finishTime = wu(-dataFromPrinter.printLeftTime / 60, !0)), dataFromPrinter.hasOwnProperty("printJobTime") && (localState.printJobTime = Tl(dataFromPrinter.printJobTime)), dataFromPrinter.hasOwnProperty("printFileName") && (localState.printFileName = dataFromPrinter.printFileName.split("/").pop(), localState.filePath = dataFromPrinter.printFileName), dataFromPrinter.hasOwnProperty("deviceState") && (localState.deviceState = dataFromPrinter.deviceState), dataFromPrinter.hasOwnProperty("hostname") && (localState.name = dataFromPrinter.hostname), dataFromPrinter.hasOwnProperty("model")) {
                    const d = Ut().data.printerPreset[dataFromPrinter.model];
                    localState.modelName = (d == null ? void 0 : d.model) || localState.modelName, localState.model = dataFromPrinter.model
                }
                if (dataFromPrinter.hasOwnProperty("feedState") && (localState.feedState = dataFromPrinter.feedState),
                    dataFromPrinter.hasOwnProperty("webrtcSupport") && (localState.webrtcSupport = dataFromPrinter.webrtcSupport),
                    localState.webrtcSupport && (localState.linuxVideoUrl = this.getLinuxVideoUrl(localState.address),
                        localState.linuxVideoUrl != "" && (localState.webrtcSupport = 0)),
                    dataFromPrinter.hasOwnProperty("materialState") && (localState.materialState = dataFromPrinter.materialState),
                    dataFromPrinter.hasOwnProperty("video") && (localState.video = dataFromPrinter.video),
                    dataFromPrinter.hasOwnProperty("withSelfTest") && (localState.withSelfTest = dataFromPrinter.withSelfTest),
                    dataFromPrinter.hasOwnProperty("printStartTime") && (localState.printStartTime = dataFromPrinter.printStartTime),
                    dataFromPrinter.hasOwnProperty("state") && (localState.state = dataFromPrinter.state,
                    (localState.state != dataFromPrinter.state && localState.err.errcode == 2839 || localState.err.key == 115) && (localState.err.errcode = 0, localState.err.key = 0, this.reconnectWebSocket(localState)), localState.state = dataFromPrinter.state),
                    dataFromPrinter.hasOwnProperty("layer") && (localState.layer = dataFromPrinter.layer),
                    dataFromPrinter.hasOwnProperty("TotalLayer") && (localState.TotalLayer = dataFromPrinter.TotalLayer),
                    dataFromPrinter.hasOwnProperty("materialCutterState") && (localState.materialCutterState = dataFromPrinter.materialCutterState),
                    (dataFromPrinter.hasOwnProperty("bedTemp0") || dataFromPrinter.hasOwnProperty("nozzleTemp") || dataFromPrinter.hasOwnProperty("boxTemp")) && this.formatTemperatureData(localState, dataFromPrinter),
                    (dataFromPrinter.hasOwnProperty("current_object") || dataFromPrinter.hasOwnProperty("excluded_objects") || dataFromPrinter.hasOwnProperty("objects")) && this.formatObjectData(localState, dataFromPrinter),
                    dataFromPrinter.hasOwnProperty("boxsInfo") && this.formatBoxsInfoData(localState, dataFromPrinter),
                    dataFromPrinter.hasOwnProperty("retMaterials") && this.formatMaterialData(localState, dataFromPrinter),
                    dataFromPrinter.hasOwnProperty("modifyMaterial") && (localState.cfsCModifyMaterial = dataFromPrinter.modifyMaterial),
                    dataFromPrinter.hasOwnProperty("autohome") && (localState.ctrol.autohome = dataFromPrinter.autohome),
                    dataFromPrinter.hasOwnProperty("curPosition") && (localState.ctrol.curPosition = dataFromPrinter.curPosition),
                    dataFromPrinter.hasOwnProperty("curFeedratePct") && (localState.ctrol.curFeedratePct = dataFromPrinter.curFeedratePct),
                    dataFromPrinter.hasOwnProperty("fan") && (localState.ctrol.fan = dataFromPrinter.fan),
                    dataFromPrinter.hasOwnProperty("modelFanPct") && (localState.ctrol.modelFanPct = dataFromPrinter.modelFanPct),
                    dataFromPrinter.hasOwnProperty("fanAuxiliary") && (localState.ctrol.fanAuxiliary = dataFromPrinter.fanAuxiliary),
                    dataFromPrinter.hasOwnProperty("auxiliaryFanPct") && (localState.ctrol.auxiliaryFanPct = dataFromPrinter.auxiliaryFanPct),
                    dataFromPrinter.hasOwnProperty("fanCase") && (localState.ctrol.fanCase = dataFromPrinter.fanCase),
                    dataFromPrinter.hasOwnProperty("caseFanPct") && (localState.ctrol.caseFanPct = dataFromPrinter.caseFanPct),
                    dataFromPrinter.hasOwnProperty("lightSw") && (localState.ctrol.lightSw = dataFromPrinter.lightSw),
                    dataFromPrinter.hasOwnProperty("boxConfig") && (localState.boxConfig = dataFromPrinter.boxConfig),
                    dataFromPrinter.hasOwnProperty("materialDetector1") && (localState.materialDetector1 = dataFromPrinter.materialDetector1),
                    dataFromPrinter.hasOwnProperty("cfsConnect") && (localState.cfsConnect = dataFromPrinter.cfsConnect),
                    dataFromPrinter.hasOwnProperty("modelVersion") && (localState.modelVersion = dataFromPrinter.modelVersion),
                    dataFromPrinter.hasOwnProperty("cmd") && dataFromPrinter.cmd == "refreshBox" && (localState.refreshBox = dataFromPrinter.result.code),
                    localState.hasOwnProperty("boxsInfo") && localState.boxsInfo.hasOwnProperty("boxColorInfo") && localState.boxsInfo.boxColorInfo.length > 0 && (localState.IsMultiColorDevice = !0),
                    localState.identity != null && localState.deviceType == 0) {

                    let d = this.getPrinter(localState.identity, !1, 1);
                    d && d.online && Object.keys(localState).forEach(u => {
                        try {
                            u != "deviceType" && u != "address" && u != "identity" && u != "online" && u != "tbId" && u != "name" && u != "socket" && (d[u] = ay.cloneDeep(localState[u]))
                        } catch (f) {
                            console.log(f)
                        }
                    })
                    
                }
                return !0
            }
}
```

```JS
static setDataFromKlipperState(t, r) {
    var n, a, i, o, l;
    if (!r) return !1;
    if (t.name || (t.name = t.model), r.heater_bed && (t.data.targetBedTemp0 = ((n = r.heater_bed) == null ? void 0 : n.target) ?? 0, t.data.bedTemp0 = ((a = r.heater_bed) == null ? void 0 : a.temperature) ?? 0), r.extruder && (t.data.targetNozzleTemp = ((i = r.extruder) == null ? void 0 : i.target) ?? 0, t.data.nozzleTemp = ((o = r.extruder) == null ? void 0 : o.temperature) ?? 0), r.print_stats && (t.printFileName = ((l = r.print_stats) == null ? void 0 : l.filename) ?? ""), t.KlipperUrl || (t.KlipperUrl = `http://${t.address.split("(")[0]}:${t.fluiddPort}`), t.online = !0, t.data.bedTemp0 === 0 && t.data.nozzleTemp === 0 && (t.online = !1), r.gcode_move && typeof r.gcode_move.speed_factor == "number" && (t.ctrol.curFeedratePct = r.gcode_move.speed_factor * 100), r.display_status && typeof r.display_status.progress == "number" && (t.printProgress = Math.floor(r.display_status.progress * 100)), r.print_stats && typeof r.print_stats.print_duration == "number") {
        const s = Math.round(r.print_stats.print_duration);
        if (t.printJobTime = Tl(s), t.printProgress > 0) {
            const c = s / (t.printProgress / 100),
                d = Math.round(c - s);
            t.printLeftTime = Tl(d)
        }
    }
    if (t.deviceState = 1e4, r.print_stats && r.print_stats.state) switch (r.print_stats.state) {
        case "standby":
            t.state = 0, t.deviceState = 0;
            break;
        case "printing":
            t.state = 1, t.deviceState = 1;
            break;
        case "complete":
            t.state = 2, t.deviceState = 0;
            break;
        case "error":
            t.state = 3, t.deviceState = 0;
            break;
        case "cancelled":
            t.state = 4, t.deviceState = 0;
            break;
        case "paused":
            t.state = 5, t.deviceState = 1;
            break;
        default:
            t.state = 0
    }
    return !0
}
```

```JS
addPrinter(t, r, n = !1) {
    const a = Ut().data.printerPreset[t.model];
    let i = {
        deviceType: t.hasOwnProperty("deviceType") ? t.deviceType : 0,
        identity: null,
        tbId: t.hasOwnProperty("tbId") ? t.tbId : null,
        timeStamp: -1,
        boxInfoTimeStamp: -1,
        modelName: a == null ? void 0 : a.name,
        deviceImg: "./img/machine/" + t.model + ".png",
        defaultDeviceImg: "./img/printerImgDefault.svg",
        name: t.name,
        state: 0,
        filePath: "",
        deviceState: t.hasOwnProperty("deviceState") ? t.deviceState : 1e4,
        printProgress: t.hasOwnProperty("printProgress") ? t.printProgress : 0,
        printStartTime: 0,
        printLeftTime: t.hasOwnProperty("printLeftTime") ? t.printLeftTime : "",
        printJobTime: t.hasOwnProperty("printJobTime") ? t.printJobTime : "",
        finishTime: 0,
        printFileName: t.hasOwnProperty("printFileName") ? t.printFileName : "",
        previewimg: t.hasOwnProperty("previewimg") ? t.previewimg : "",
        address: t.address,
        mac: t.mac,
        model: t.model,
        visable: !0,
        type: t.type,
        socket: t.socket,
        IsMultiColorDevice: t.hasOwnProperty("IsMultiColorDevice") ? t.IsMultiColorDevice : !1,
        feedState: 0,
        webrtcSupport: 0,
        materialDetector1: t.hasOwnProperty("materialDetector1") ? t.materialDetector1 : 0,
        modelVersion: t.modelVersion,
        online: t.online,
        connected: !1,
        reconnectCount: 0,
        layer: "-",
        TotalLayer: "-",
        repoPlrStatus: 0,
        materialStatus: 0,
        machinePlatformMotionEnable: 1,
        auxiliary_fan: 0,
        machine_LED_light_exist: 0,
        support_air_filtration: 0,
        video: t.video,
        oldPrinter: t.oldPrinter ? t.oldPrinter : !1,
        moonrakerPort: t.hasOwnProperty("moonrakerPort") ? t.moonrakerPort : 0,
        fluiddPort: t.hasOwnProperty("fluiddPort") ? t.fluiddPort : 0,
        mainsailPort: t.hasOwnProperty("mainsailPort") ? t.mainsailPort : 0,
        KlipperUrl: t.hasOwnProperty("KlipperUrl") ? t.KlipperUrl : "",
        withSelfTest: 100,
        linuxVideoUrl: "",
        materialCutterState: -1,
        err: {
            errcode: 1,
            key: 0,
            value: "",
            errLevel: 0
        },
        refreshBox: 0,
        ctrol: {
            autohome: "X:0 Y:0 Z:0",
            curPosition: "X:1 Y:1 Z:1",
            curFeedratePct: 0,
            speedMode: 1,
            fan: 0,
            modelFanPct: 0,
            fanAuxiliary: 0,
            auxiliaryFanPct: 0,
            fanCase: 0,
            caseFanPct: 0,
            lightSw: 0
        },
        temperature: {
            record: !1,
            time: [],
            nozzle: {
                value: 0,
                target: 0,
                max: 0,
                data: []
            },
            bed: {
                value: 0,
                target: 0,
                max: 0,
                data: []
            },
            box: {
                value: 0,
                target: 0,
                max: 0,
                data: []
            }
        },
        boxsInfo: {
            same_material: [],
            color_same_material: [],
            boxColorInfo: [],
            materialBoxs: [],
            cfsName: ""
        },
        boxConfig: {
            cAutoFeed: 0,
            cSelfTest: 0,
            autoRefill: 0,
            ignoreColorAutoFeed: 0
        },
        part: {
            current_object: "",
            excluded_objects: [],
            objects: []
        },
        materials: [],
        data: {},
        materialState: null,
        selected: !1,
        openCFS: !1,
        printCalibration: !1,
        uploadGCodeProgress: 0,
        uploadSpeed: 0,
        isCurrentDevice: !1,
        isRelatedToAccount: !1,
        cfsConnect: 0,
        uploadFileProgress: 0,
        uploadState: 0
    };
    if (Ut().currentDevice != "")
        for (const o of Ut().data.printerList) {
            let l = !1;
            for (let s of o.list) Ut().currentDevice == s.mac && (l = !0, s.type == 1);
            if (l) break
        }
    if (i.deviceType == 0 || i.deviceType == 1) {
        let o = this.getPrinter(i.mac, !0);
        o != null && (o.deviceType == 0 && o.online || o.deviceType == 1 && i.online) && (o.identity = i.address, i.identity = o.address)
    }
    this.getMachinePlatformMotionEnable(t);
    for (const o of Ut().data.printerList)
        for (let l of o.list) l.mac == t.mac && l.deviceType == 0 && l.online;
    for (const o of Ut().data.printerList)
        if (o.group === r) {
            i.online && !n ? o.list.unshift(i) : o.list.push(i);
            break
        }! i.online || i.socket == null
}
```