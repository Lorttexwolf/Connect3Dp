# BambuLab

Parsing data from MQTT.

- Most of the naming of these properties don't make sense at face value.

## Print Job

```
"job_id": "360562969",
```

## Extruders

### X1C, X1E, P1

These models firmware don't account for the possibility of multiple nozzles.

### H2D, H2S, H2C, P2S


## Extruders & Nozzles

### X1C, X1E, P1

```
@print
```
```json
{
    "nozzle_diameter": "0.4",
    "nozzle_type": "stainless_steel", 
    "nozzle_temper": 27.8125,
    "nozzle_target_temper": 0
}
```

```
@print.ams
```
```json
{
    "tray_now": "255"
}
```

### H2D, H2S, H2C, P2S

```
@print.device.nozzle.info[i]
```
```json
{
    "diameter": 0.4,
    "id": 0,
    "tm": 0,
    "type": "HS01",
    "wear": 0
}
```

[Bambu Studio Nozzle Parsing Source](https://github.com/bambulab/BambuStudio/blob/9c30cf25188793b6e37e64bf035bab7a0feff13a/src/slic3r/GUI/DeviceCore/DevNozzleSystem.cpp#L732)

```
print.device.extruder.info[i]
```
```json
{
    "filam_bak": [],
    "hnow": 0,
    "hpre": 0,
    "htar": 0,
    "id": 0,
    "info": 78,
    "snow": 3,
    "spre": 3,
    "star": 3,
    "stat": 197376,
    "temp": 16711934
}
```

`hnow` The ID of the current hot end / nozzle loaded, the induvial nozzle can be found by matching the value at `print.device.nozzle.info[i].id`.

[Bambu Studio Extruder Parsing Source](https://github.com/bambulab/BambuStudio/blob/master/src/slic3r/GUI/DeviceCore/DevExtruderSystem.cpp#L245)

<hr/>

## 3MF

### Determining Filament Usage from a 3MF File

A Bambu Lab 3MF file is a ZIP archive. While it is loosely based on the Microsoft 3MF specification, it contains a number of Bambu-specific extensions and deviations.

Slice metadata is stored in: `Metadata/slice_info.config`

This file is an XML document containing per-plate slicing and material information.

**Example**
```xml
<?xml version="1.0" encoding="UTF-8"?>
<config>
  <header>
    <header_item key="X-BBL-Client-Type" value="slicer"/>
    <header_item key="X-BBL-Client-Version" value="02.05.00.64"/>
  </header>
  <plate>
    <metadata key="index" value="1"/>
    <metadata key="extruder_type" value="0"/>
    <metadata key="nozzle_volume_type" value="0"/>
    <metadata key="printer_model_id" value="N7"/>
    <metadata key="nozzle_diameters" value="0.4"/>
    <metadata key="timelapse_type" value="0"/>
    <metadata key="prediction" value="26397"/>
    <metadata key="weight" value="263.68"/>
    <metadata key="first_layer_time" value="437.053558"/>
    <metadata key="outside" value="false"/>
    <metadata key="support_used" value="true"/>
    <metadata key="label_object_enabled" value="false"/>
    <metadata key="filament_maps" value="1 1 1"/>
    <metadata key="limit_filament_maps" value="0 0 0"/>
    <object identify_id="128" name="Regal-Iron_Lung-Blood.stl" skipped="false" />
    <filament id="2" tray_info_idx="GFA01" type="PLA" color="#BB3D43" used_m="83.05" used_g="263.68" used_for_object="true" used_for_support="true" group_id="0" nozzle_diameter="0.40" volume_type="Standard"/>
    <warning msg="bed_temperature_too_high_than_filament" level="3" error_code ="1000C001"  />
    <layer_filament_lists>
      <layer_filament_list filament_list="1" layer_ranges="0 620" />
    </layer_filament_lists>
  </plate>
</config>
```

Filament usage is described by the `<filament />` elements within the `<plate />` element. Each `<filament />` entry corresponds to a single material source (e.g., AMS tray).