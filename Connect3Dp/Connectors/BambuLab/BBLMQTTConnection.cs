using Connect3Dp.Connectors.BambuLab.Constants;
using Connect3Dp.Extensions;
using Connect3Dp.State;
using Connect3Dp.Utilities;
using MQTTnet;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Connect3Dp.Connectors.BambuLab
{
    internal class BBLMQTTConnection
    {
        private readonly Logger Logger;

        private readonly IMqttClient MQTTClient = new MqttClientFactory().CreateMqttClient();

        public event Action<BBLMQTTData>? OnData;

        public readonly IPAddress Address;
        public readonly string SN;
        public readonly string AccessCode;

        public bool IsConnected => MQTTClient.IsConnected;

        private PeriodicAsyncAction? PullAllChangesPeriodic;

        private readonly Dictionary<int, string> AMSIDToSN = [];
        private readonly Dictionary<string, int> SNToAMSID = [];

        public BBLMQTTConnection(IPAddress address, string sn, string accessCode)
        {
            this.Address = address;
            this.SN = sn;
            this.AccessCode = accessCode;
            this.Logger = Logger.OfCategory($"{nameof(BBLMQTTConnection)} {this.SN}");

            MQTTClient.ConnectedAsync += OnConnected;
            MQTTClient.DisconnectedAsync += OnDisconnected;
            MQTTClient.ApplicationMessageReceivedAsync += OnMessage;
        }

        public async Task Connect(CancellationToken cancellationToken = default)
        {
            if (this.MQTTClient.IsConnected) return;

            await this.MQTTClient.ConnectAsync(this.MQTTOptions, cancellationToken);
            await this.MQTTClient.PingAsync(cancellationToken);
        }

        private async Task PublishCommand(string category, string command, JsonObject? commandData = null)
        {
            if (!this.IsConnected) return;
            commandData ??= new JsonObject();

            commandData.Add("command", command);
            commandData.Add("sequence_id", "0");
            commandData.Add("timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds());

            var data = new JsonObject
            {
                { category, commandData }
            };
            await this.MQTTClient.PublishStringAsync(BBLConstants.MQTT.RequestTopic(this.SN), data.ToJsonString());
        }

        public async Task PublishClearBed()
        {
            await this.PublishCommand("print", "bed_clean");
        }

        //public async Task PublishTraySetting(BBLFilament filament)
        //{
        //    var commandData = new JsonObject
        //    {
        //        { "ams_id", filament.Location.IsAutomatic ? (int)Math.Floor(filament.Location.Slot / 4d) : 255 },
        //        { "nozzle_temp_max", filament.NozzleTempMax },
        //        { "nozzle_temp_min", filament.NozzleTempMin },
        //        { "setting_id", filament.SettingID },
        //        { "slot_id", filament.Location.Slot % 4 },
        //        { "tray_color", $"{filament.Base.Color.Hex}FF" },
        //        { "tray_id", filament.TrayID },
        //        { "tray_info_idx", filament.TrayInfoIDX },
        //        { "tray_type", filament.Base.Material }
        //    };
        //    await this.PublishCommand("print", "ams_filament_setting", commandData);
        //}

        public async Task PublishClearError(int errorCode)
        {
            var commandData = new JsonObject
            {
                { "print_error", errorCode }
            };
            await this.PublishCommand("print", "clean_print_error", commandData);
        }

        public async Task PublishStop()
        {
            await this.PublishCommand("print", "stop");
        }

        public async Task PublishPause()
        {
            await this.PublishCommand("print", "pause");
        }

        public async Task PublishResume()
        {
            await this.PublishCommand("print", "resume");
        }

        public async Task PublishLEDControl(string fixtureName, bool isOn)
        {
            var commandData = new JsonObject
            {
                { "led_mode", isOn ? "on" : "off" },
                { "led_node", fixtureName },
                { "interval_time", 500 },
                { "led_off_time", 250 },
                { "led_on_time", 250 },
                { "loop_times", 30 }
            };

            await this.PublishCommand("system", "ledctrl", commandData);
        }

        public Task PublishAMSHeatingCommand(string amsSN, HeatingSettings settings)
        {
            // https://github.com/greghesp/ha-bambulab/issues/1448
            var commandData = new JsonObject
            {
                { "duration", settings.Duration.TotalHours },
                { "humidity", 0 },
                { "ams_id", SNToAMSID[amsSN] },
                { "mode", 1 },
                { "rotate_tray", settings.DoSpin },
                { "temp", settings.TempC },
                { "cooling_temp", 40 } // Unsure what this field does. 

            };
            return this.PublishCommand("print", "ams_filament_drying", commandData);
        }

        public Task PublishAMSStopHeatingCommand(string amsSN)
        {
            // https://github.com/greghesp/ha-bambulab/issues/1448
            var commandData = new JsonObject
            {
                { "duration", 0 },
                { "humidity", 0 },
                { "ams_id", SNToAMSID[amsSN] },
                { "mode", 0 },
                { "rotate_tray", false },
                { "temp", 0 },
                { "cooling_temp", 40 } // Unsure what this field does. 

            };
            return this.PublishCommand("print", "ams_filament_drying", commandData);
        }

        public Task PublishHVACModeCommand(MachineAirDuctMode mode)
        {
            var commandData = new JsonObject
            {
                { "modeId", mode == MachineAirDuctMode.Heating ? 1 : 0 }
            };
            return this.PublishCommand("print", "set_airduct", commandData);
        }

        public async Task PublishGetFirmwareVersion()
        {
            await this.PublishCommand("info", "get_version");
        }

        public async Task PublishPushAll()
        {
            await this.PublishCommand("pushing", "pushall");
        }

        private async Task OnConnected(MqttClientConnectedEventArgs ev)
        {
            await this.MQTTClient.SubscribeAsync(BBLConstants.MQTT.ReportTopic(this.SN));

            this.OnData?.Invoke(new BBLMQTTData
            {
                Changes = new MachineStateUpdate().SetConnected(true)
            });

            await this.PublishGetFirmwareVersion();
            await this.PublishPushAll();

            this.PullAllChangesPeriodic ??= new PeriodicAsyncAction(TimeSpan.FromMinutes(15), PublishPushAll);
        }

        private async Task OnDisconnected(MqttClientDisconnectedEventArgs ev)
        {
            this.OnData?.Invoke(new BBLMQTTData
            {
                Changes = new MachineStateUpdate().SetConnected(false)
            });

            if (this.PullAllChangesPeriodic != null)
            {
                await this.PullAllChangesPeriodic.DisposeAsync();
                this.PullAllChangesPeriodic = null;
            }

            _ = ContinuouslyConnect();
        }

        private Task ContinuouslyConnect()
        {
            return Task.Run(async () =>
            {
                while (!this.MQTTClient.IsConnected)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    try
                    {
                        await this.MQTTClient.ReconnectAsync();
                    }
                    catch (Exception)
                    {
                        ;
                    }
                }
            });
        }

        private async Task OnMessage(MqttApplicationMessageReceivedEventArgs ev)
        {
            var JSON = JsonDocument.Parse(ev.ApplicationMessage.Payload);
            var readData = new BBLMQTTData(new MachineStateUpdate(), null, null, null, null);

            if (JSON.RootElement.TryGetProperty("print", out var printJSON))
            {
                //if (printJSON.TryGetInt32(out var msg, "msg") && msg == 1)
                //{
                //    // TODO: Implement for A1
                //    return;
                //}

                OnMessagePrintMQTTSecurity(printJSON, ref readData);

                OnMessagePrintSDCard(printJSON, ref readData);

                OnMessagePrintGcodeState(printJSON, ref readData);

                OnMessagePrintJob(printJSON, ref readData);

                if (printJSON.TryGetProperty("device", out var devicesJSON))
                {
                    OnMessagePrintDevices(devicesJSON, ref readData);
                }

                OnMessagePrintMaterials(printJSON, ref readData);

                OnMessagePrintNozzles(printJSON, ref readData);

                OnMessagePrintLighting(printJSON, ref readData);
            }

            if (JSON.RootElement.TryGetProperty("info", out var infoJSON))
            {
                OnMessageInfoVersion(infoJSON, ref readData);
            }

            if (readData.UpdateAMSMapping.HasValue && readData.UpdateAMSMapping.Value)
            {
                await this.PublishGetFirmwareVersion();
                Logger.Trace("AMS ID -> AMS SN mappings requested.");
            }

            OnData?.Invoke(readData);
        }

        private static void OnMessagePrintDevices(JsonElement devicesJSON, ref BBLMQTTData data)
        {
            if (devicesJSON.TryGetPropertyChain(out var modeCurElem, "airduct", "modeCur"))
            {
                data.Changes.SetAirDuctMode(modeCurElem.GetInt32() == 1 ? MachineAirDuctMode.Heating : MachineAirDuctMode.Cooling);
            }
        }

        private static void OnMessagePrintMQTTSecurity(JsonElement printJSON, ref BBLMQTTData data)
        {
            // TODO: NOT WORKING

            bool isResultFailed = printJSON.TryGetProperty("result", out var resultElem) && resultElem.GetString()!.Equals("failed", StringComparison.OrdinalIgnoreCase);
            bool isReasonMsgSecurityFailed = printJSON.TryGetProperty("reason", out var failedReasonElem) && failedReasonElem.GetString()!.Equals(BBLConstants.MQTT.SECURITY_FAILED_ERROR_MSG, StringComparison.OrdinalIgnoreCase);

            if (isResultFailed && isReasonMsgSecurityFailed)
            {
                // mqtt message verify failed indicates we do not have permission to control this machine.
                data.UsesUnsupportedSecurity = true;
            }
        }

        private void OnMessagePrintGcodeState(JsonElement printJSON, ref BBLMQTTData data)
        {
            if (printJSON.TryGetString(out var gcode_state, "gcode_state"))
            {
                Logger.Trace($"{nameof(gcode_state)}: {gcode_state}");

                data.Changes.SetStatus(gcode_state.ToLower() switch
                {
                    "idle" => MachineStatus.Idle,
                    "running" => MachineStatus.Printing,
                    "pause" => MachineStatus.Paused,
                    "finish" => MachineStatus.Printed,
                    "failed" => MachineStatus.Failed,
                    _ => MachineStatus.Unknown,
                });
            }

            if (printJSON.TryGetInt32(out var print_error, "print_error"))
            {
                Logger.Trace($"{nameof(print_error)}: {print_error}");

                if (print_error != 0 && data.Changes.Status == MachineStatus.Printing)
                {
                    // Sometimes machine reports running when print_error is present..?
                    data.Changes.SetStatus(MachineStatus.Paused);
                }
            }

            //"print_type" Laser? FDM?
        }

        private void OnMessagePrintJob(JsonElement printJSON, ref BBLMQTTData data)
        {
            if (printJSON.TryGetInt32(out var mc_percent, "mc_percent"))
            {
                data.Changes.UpdateCurrentJob(changes => changes.SetPercentageComplete(mc_percent));
            }

            if (printJSON.TryGetInt32(out var stg_cur, "stg_cur"))
            {
                Logger.Trace($"{nameof(stg_cur)}: {stg_cur}");

                data.Changes.UpdateCurrentJob(changes => changes.SetStage(stg_cur switch
                {
                    BBLConstants.PrintStages.COOLING_CHAMBER => "Cooling Chamber",
                    BBLConstants.PrintStages.IDENTIFYING_BUILD_PLATE => "Identifying Build Plate",
                    BBLConstants.PrintStages.HOMING_TOOLHEAD_STAGE => "Homing Toolhead",
                    BBLConstants.PrintStages.CHANGING_FILAMENT => "Changing Filament",
                    _ => "Printing"
                }));
            }

            if (printJSON.TryGetString(out var subtask_name, "subtask_name"))
            {
                var processedName = string.IsNullOrWhiteSpace(subtask_name) ? "None" : subtask_name;

                data.Changes.UpdateCurrentJob(changes => changes.SetName(processedName));
            }

            if (printJSON.TryGetInt32(out var mc_remaining_time, "mc_remaining_time"))
            {
                var remainingTimeSpan = TimeSpan.FromSeconds(mc_remaining_time);

                data.Changes.UpdateCurrentJob(changes => changes.SetRemainingTime(remainingTimeSpan));
            }

            if ((data.Changes.CurrentJobUpdate?.PercentageComplete.HasValue  ?? false)
                && (data.Changes.CurrentJobUpdate?.RemainingTime.HasValue ?? false)
                && TryCalculateTotalTime(data.Changes.CurrentJobUpdate.RemainingTime.Value.TotalSeconds, data.Changes.CurrentJobUpdate.PercentageComplete.Value, out var calculatedTotalTime))
            {
                data.Changes.UpdateCurrentJob(changes => changes.SetTotalTime(calculatedTotalTime.Value));
            }
        }

        private void OnMessagePrintMaterials(JsonElement printJSON, ref BBLMQTTData data)
        {
            if (printJSON.TryGetPropertyChain(out var amsArrayElem, "ams", "ams"))
            {
                var amsCount = amsArrayElem.GetArrayLength();

                foreach (var amsElem in amsArrayElem.EnumerateArray())
                {
                    if (!amsElem.TryGetString(out var amsId, "id")) continue;

                    if (!AMSIDToSN.TryGetValue(int.Parse(amsId), out var amsSN))
                    {
                        // Missing mapping, request get_version.
                        data.UpdateAMSMapping = true;

                        Logger.Warning($"Missing AMS ID to SN Mapping for AMS #{amsId}");

                        continue;
                    }

                    var amsModel = BBLConstants.GetAMSModelFromSN(amsSN);
                    var amsFeatures = BBLConstants.GetAMSFeaturesFromModel(amsModel);
                    var amsHeatingConstraints = BBLConstants.GetAMSHeatingConstraintsFromModel(amsModel);

                    data.Changes.UpdateMaterialUnit(amsSN, amsConfigure => amsConfigure
                        .SetCapacity(BBLConstants.GetAMSCapacityFromModel(amsModel))
                        .SetModel(amsModel)
                        .SetFeatures(amsFeatures)
                        .SetHeatingConstraints(amsHeatingConstraints));

                    // Loaded Materials

                    if (amsElem.TryGetProperty("tray", out var traysElem))
                    {
                        foreach (var trayElem in traysElem.EnumerateArray())
                        {
                            if (!trayElem.TryGetString(out var trayId_s, "id") || !int.TryParse(trayId_s, out var trayId)) continue;

                            if (trayElem.TryGetInt32(out var state, "state") && state == 10)
                            {
                                // Tray is empty! Though, this doesn't work for A1/P1 series.

                                data.Changes.UpdateMaterialUnit(amsSN, amsConfigure => amsConfigure.ClearSlot(trayId));
                                continue;
                            }

                            if (trayElem.TryGetString(out var tray_type, "tray_type")
                                && trayElem.TryGetString(out var tray_color, "tray_color")
                                && trayElem.TryGetString(out var tray_info_idx, "tray_info_idx"))
                            {
                                var trayMaterial = new Material(tray_type, new MaterialColor(null, tray_color), tray_info_idx);

                                data.Changes.UpdateMaterialUnit(amsSN, amsConfigure => amsConfigure.SetSlot(trayId, trayMaterial));
                            }
                        }
                    }  

                    if (amsElem.TryGetString(out var temp_C_s, "temp") && double.TryParse(temp_C_s, out var temp_C))
                    {
                        data.Changes.UpdateMaterialUnit(amsSN, configure => configure.SetTemperatureC(temp_C));
                    }

                    if (amsElem.TryGetString(out var humidity_percent_S, "humidity_raw") && Int32.TryParse(humidity_percent_S, out var humidity_percent))
                    {
                        data.Changes.UpdateMaterialUnit(amsSN, configure => configure.SetHumidityPercent(humidity_percent));
                    }

                    if (amsElem.TryGetInt32(out var dry_time, "dry_time"))
                    {
                        // Bambu Lab doesn't support target temp, nor is spinning as of 1/18/2026 :skull:

                        if (dry_time > 0)
                        {
                            var activeHeatingSettings = new PartialHeatingSettings()
                            {
                                Duration = TimeSpan.FromMinutes(dry_time)
                            };

                            data.Changes.UpdateMaterialUnit(amsSN, configure => configure.SetActiveHeatingSettings(activeHeatingSettings));
                        }
                        else
                        {
                            data.Changes.UpdateMaterialUnit(amsSN, configure => configure.SetActiveHeatingSettings(null));
                        }

                    }
                }

            }
        }

        private void OnMessagePrintNozzles(JsonElement printJSON, ref BBLMQTTData data)
        {
            // TODO: Improve.

            var materialInNozzles = new Dictionary<int, MaterialLocation>();

            if (printJSON.TryGetPropertyChain(out var extruders, "device", "extruder", "info"))
            {
                foreach (var extruder in extruders.EnumerateArray())
                {
                    // {
                    //  "filam_bak": [ 48, 192 ],
                    //  "hnow": 0,
                    //  "hpre": 0,
                    //  "htar": 0,
                    //  "id": 0,
                    //  "info": 74,
                    //  "snow": 258,
                    //  "spre": 258,
                    //  "star": 258,
                    //  "stat": 197376,
                    //  "temp": 14418140
                    //}

                    if (extruder.TryGetInt32(out int number, "id") && extruder.TryGetInt32(out var snow, "snow"))
                    {
                        ParseTrayAndAMSInt32((ushort)snow, out var ams, out var tray);

                        if (AMSIDToSN.TryGetValue(ams, out var amsSN))
                        {
                            materialInNozzles.Add(number, new MaterialLocation(amsSN, tray));
                        }
                        else
                        {
                            data.UpdateAMSMapping = true;
                        }
                    }
                }
            }

            if (printJSON.TryGetPropertyChain(out var nozzles, "device", "nozzle", "info"))
            {
                foreach (var nozzle in nozzles.EnumerateArray())
                {
                    if (nozzle.TryGetInt32(out int number, "id") && nozzle.TryGetDouble(out double diameter, "diameter"))
                    {
                        var materialInNozzle = materialInNozzles.GetValueOrDefault(number);

                        data.Changes.SetNozzle(new MachineNozzle(number, diameter, materialInNozzle == default ? null : materialInNozzle));
                    }

                }
            }
        }

        private void OnMessagePrintHMS(JsonElement printJSON, ref BBLMQTTData data)
        {
            // Sample
            // print.hms
            // "hms": [
            // {
            //     "attr": 83886848,
            //    "code": 131086
            // }
            // ],

        }

        private void OnMessagePrintSDCard(JsonElement printJSON, ref BBLMQTTData data)
        {
            if (printJSON.TryGetBoolean(out bool sdcard, "sdcard"))
            {
                data.HasUSBOrSDCard = sdcard;
            }
        }


        private static void OnMessagePrintLighting(JsonElement printJSON, ref BBLMQTTData data)
        {
            //"lights_report": [
            //  {
            //    "mode": "on",
            //    "node": "chamber_light"
            //  },
            //  {
            //    "mode": "flashing",
            //    "node": "work_light"
            //  }
            //],

            if (printJSON.TryGetProperty("lights_report", out var lights_report))
            {
                foreach (var light_fixture in lights_report.EnumerateArray())
                {

                    if (light_fixture.TryGetString(out string name, "node")
                        && light_fixture.TryGetString(out string mode, "mode"))
                    {

                        var isOn = !mode.Equals("off");

                        if (name.Equals("chamber_light"))
                        {
                            data.Changes.SetLight("Chamber", isOn);
                        }
                    }


                }
            }

        }
        public static void ParseTrayAndAMSInt32(ushort value, out ushort ams, out ushort tray)
        {
            uint u = (uint)value;
            tray = (ushort)(u & 0xFF); // bits 0..7
            ams = (ushort)((u >> 8) & 0xFF); // bits 8..15
        }

        private struct BBLModule
        {
            [JsonPropertyName("name")]
            public string InternalName { get; set; }

            [JsonPropertyName("product_name")]
            public string ProductName { get; set; }

            [JsonPropertyName("sw_ver")]
            public BBLFirmwareVersion Version { get; set; }

            [JsonPropertyName("sn")]
            public string SN { get; set; }
        }

        private void OnMessageInfoVersion(JsonElement infoJSON, ref BBLMQTTData data)
        {
            if (!infoJSON.TryGetString(out var command, "command") ||
                !command.Equals("get_version", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!infoJSON.TryGetProperty("module", out var moduleProperty))
            {
                return;
            }

            foreach (var moduleJSON in moduleProperty.EnumerateArray())
            {
                var module = JsonSerializer.Deserialize<BBLModule>(moduleJSON);

                if (module.InternalName.Equals("ota", StringComparison.OrdinalIgnoreCase))
                {
                    data.FirmwareVersion = module.Version;
                    Logger.Trace($"Machine Firmware Version {data.FirmwareVersion}");
                }
                else
                {
                    TryParseAMSModule(module);
                }
            }
        }

        private void TryParseAMSModule(BBLModule module)
        {
            var amsPrefixes = new[]
            {
                ("n3f/", 4),      // AMS 2 Pro
                ("ams_f1/", 7),   // AMS Lite
                ("n3s/", 4),      // AMS HT
                ("ams/", 4)       // AMS
            };

            foreach (var (prefix, length) in amsPrefixes)
            {
                if (module.InternalName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(module.InternalName.AsSpan(length), out int amsId))
                    {
                        AMSIDToSN[amsId] = module.SN;
                        SNToAMSID[module.SN] = amsId;
                    }
                    break;
                }
            }
        }

        public static bool TryCalculateTotalTime(double secondsRemaining, double percentCompleted, [NotNullWhen(true)] out TimeSpan? totalTime)
        {
            totalTime = null;

            if (percentCompleted >= 100) return false;

            if (percentCompleted < 0) return false;

            double percentRemaining = (100.0 - percentCompleted) / 100.0;

            if (percentRemaining <= 0) return false;

            totalTime = TimeSpan.FromSeconds((long)(secondsRemaining / percentRemaining));

            return true;
        }

        private MqttClientOptions MQTTOptions => new MqttClientOptionsBuilder()
            .WithTcpServer(this.Address.ToString(), 8883)
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
            .WithClientId("Connect3Dp")
            .WithCleanSession(true)
            .WithProtocolType(System.Net.Sockets.ProtocolType.Tcp)
            .WithCredentials("BBLP".ToLower(), this.AccessCode)
            .WithTimeout(TimeSpan.FromSeconds(15))
            .WithTlsOptions(new MqttClientTlsOptions
            {
                UseTls = true,
                AllowUntrustedCertificates = true,
                AllowRenegotiation = true,
                IgnoreCertificateChainErrors = true,
                CertificateValidationHandler = (cV) =>
                {
                    return true;
                }
            })
            .Build();
    }

    internal record struct BBLMQTTData(MachineStateUpdate Changes, BBLFirmwareVersion? FirmwareVersion, bool? UsesUnsupportedSecurity, bool? UpdateAMSMapping, bool? HasUSBOrSDCard);
}