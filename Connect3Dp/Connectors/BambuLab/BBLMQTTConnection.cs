using Connect3Dp;
using Connect3Dp.Connectors.BambuLab;
using Connect3Dp.Utilities;
using MQTTnet;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        private Dictionary<int, string> AMSIDToSN = new();

        public BBLMQTTConnection(IPAddress address, string sn, string accessCode)
        {
            this.Address = address;
            this.SN = sn;
            this.AccessCode = accessCode;
            this.Logger = new($"{nameof(BBLMQTTConnection)} {this.SN}");

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

        public Task PublishAMSHeatingCommand(int amsID, HeatingSettings settings)
        {
            // https://github.com/greghesp/ha-bambulab/issues/1448
            var commandData = new JsonObject
            {
                { "duration", settings.Duration.TotalHours },
                { "humidity", 0 },
                { "ams_id", amsID },
                { "mode", 1 },
                { "rotate_tray", settings.DoSpin },
                { "temp", settings.TempC },
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
                Connection = true
            });

            await this.PublishGetFirmwareVersion();
            await this.PublishPushAll();

            this.PullAllChangesPeriodic ??= new PeriodicAsyncAction(TimeSpan.FromMinutes(15), PublishPushAll);
        }

        private async Task OnDisconnected(MqttClientDisconnectedEventArgs ev)
        {
            this.OnData?.Invoke(new BBLMQTTData
            {
                Connection = false
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

        private Task OnMessage(MqttApplicationMessageReceivedEventArgs ev)
        {
            var JSON = JsonDocument.Parse(ev.ApplicationMessage.Payload);
            var readData = new BBLMQTTData();

            if (JSON.RootElement.TryGetProperty("print", out var printJSON))
            {
                OnMessagePrintMQTTSecurity(printJSON, ref readData);

                OnMessagePrintGcodeState(printJSON, ref readData);

                OnMessagePrintJob(printJSON, ref readData);

                if (printJSON.TryGetProperty("device", out var devicesJSON))
                {
                    OnMessagePrintDevices(devicesJSON, ref readData);
                }

                OnMessagePrintMaterials(printJSON, ref readData);
            }

            if (JSON.RootElement.TryGetProperty("info", out var infoJSON))
            {
                OnMessageInfoVersion(infoJSON, ref readData);
            }

            OnData?.Invoke(readData);

            return Task.CompletedTask;
        }

        private static void OnMessagePrintDevices(JsonElement devicesJSON, ref BBLMQTTData data)
        {
            if (devicesJSON.TryGetPropertyChain(out var modeCurElem, "airduct", "modeCur"))
            {
                data.HVACMode = modeCurElem.GetInt32() == 1 ? MachineAirDuctMode.Heating : MachineAirDuctMode.Cooling;
            }
        }

        private static void OnMessagePrintMQTTSecurity(JsonElement printJSON, ref BBLMQTTData data)
        {
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

                MachineStatus state = gcode_state.ToLower() switch
                {
                    "idle" => MachineStatus.Idle,
                    "running" => MachineStatus.Printing,
                    "pause" => MachineStatus.Paused,
                    "finish" => MachineStatus.Printed,
                    "failed" => MachineStatus.Failed,
                    _ => MachineStatus.Unknown,
                };

                data.Status = state;
            }

            if (printJSON.TryGetInt32(out var print_error, "print_error"))
            {
                Logger.Trace($"{nameof(print_error)}: {print_error}");

                if (print_error != 0 && data.Status == MachineStatus.Printing)
                {
                    // Sometimes machine reports running when print_error is present..?
                    data.Status = MachineStatus.Paused;
                }
            }

            //"print_type"
        }

        private void OnMessagePrintJob(JsonElement printJSON, ref BBLMQTTData data)
        {
            var printJob = new PartialMachinePrintJob();

            if (printJSON.TryGetInt32(out var mc_percent, "mc_percent"))
            {
                printJob.PercentageComplete = mc_percent;
            }

            if (printJSON.TryGetInt32(out var stg_cur, "stg_cur"))
            {
                Logger.Trace($"{nameof(stg_cur)}: {stg_cur}");

                printJob.Stage = stg_cur switch
                {
                    BBLConstants.PrintStages.COOLING_CHAMBER => "Cooling Chamber",
                    BBLConstants.PrintStages.IDENTIFYING_BUILD_PLATE => "Identifying Build Plate",
                    BBLConstants.PrintStages.HOMING_TOOLHEAD_STAGE => "Homing Toolhead",
                    BBLConstants.PrintStages.CHANGING_FILAMENT => "Changing Filament",
                    _ => "Printing"
                };

                // https://github.com/greghesp/ha-bambulab/blob/main/scripts/update_error_text.py
            }

            if (printJSON.TryGetString(out var subtask_name, "subtask_name"))
            {
                //Logger.Trace($"{nameof(subtask_name)}: {subtask_name}");

                printJob.Name = string.IsNullOrWhiteSpace(subtask_name) ? "None" : subtask_name;
            }

            if (printJSON.TryGetInt32(out var mc_remaining_time, "mc_remaining_time"))
            {
                printJob.RemainingTime = TimeSpan.FromSeconds(mc_remaining_time);
            }

            if (printJob.PercentageComplete.HasValue && printJob.RemainingTime.HasValue)
            {
                printJob.TotalTime = CalculateTotalTime(printJob.RemainingTime.Value.TotalSeconds, printJob.PercentageComplete.Value);
            }

            if (printJob.RemainingTime == TimeSpan.Zero && printJob.PercentageComplete == 0)
            {

                // Shouldn't be passible! 
                return;
            }

            if (data.Status == MachineStatus.Printing)
            {

            }
            else if (data.Status == MachineStatus.Printed)
            {
                printJob.PercentageComplete = 100;
                printJob.RemainingTime = TimeSpan.Zero;
                printJob.Stage = null;

            }
            else if (data.Status == MachineStatus.Paused)
            {

            }
            else if (data.Status == MachineStatus.Failed)
            {

            }
            else
            {
                return;
            }

            data.PrintJob = printJob;
        }

        private void OnMessagePrintMaterials(JsonElement printJSON, ref BBLMQTTData data)
        {
            if (printJSON.TryGetInt32(out var msg, "msg") && msg == 1)
            {
                // TODO: Implement for A1
                return;
            }

            bool doGetVersion = false;

            if (printJSON.TryGetPropertyChain(out var amsArrayElem, "ams", "ams"))
            {
                var amsCount = amsArrayElem.GetArrayLength();

                var bleh = new HashSet<MaterialUnit>();

                foreach (var amsElem in amsArrayElem.EnumerateArray())
                {
                    if (!amsElem.TryGetString(out var amsId, "id") || !amsElem.TryGetProperty("tray", out var traysElem) || !amsElem.TryGetString(out var info, "info"))
                    {
                        // Without an ID, info, or even the tray, is this even an AMS?
                        continue;
                    }

                    if (!AMSIDToSN.TryGetValue(int.Parse(amsId), out var amsSN))
                    {
                        // Missing mapping, request get_version.
                        doGetVersion = true;

                        Logger.Warning($"Missing AMS ID to SN Mapping for {amsSN}");

                        continue;
                    }

                    var amsModel = BBLConstants.GetAMSModelFromSN(amsSN);
                    var amsFeatures = BBLConstants.GetAMSFeaturesFromModel(amsModel);

                    var materialUnit = new MaterialUnit(amsSN, traysElem.GetArrayLength())
                    {
                        Features = amsFeatures,
                        Model = amsModel,
                        ActiveHeatingSettings = null
                    };

                    if (amsElem.TryGetString(out var temp_C_s, "temp") && double.TryParse(temp_C_s, out var temp_C))
                    {
                        materialUnit.TemperatureC = temp_C;
                    }

                    if (amsElem.TryGetString(out var humidity_percent_S, "humidity_raw") && Int32.TryParse(humidity_percent_S, out var humidity_percent))
                    {
                        materialUnit.HumidityPercent = humidity_percent;
                    }

                    if (amsElem.TryGetInt32(out var dry_time, "dry_time") && dry_time > 0)
                    {
                        // Bambu Lab doesn't support target temp, nor is spinning as of 1/18/2026 :skull:

                        materialUnit.ActiveHeatingSettings = new PartialHeatingSettings()
                        {
                            Duration = TimeSpan.FromMinutes(dry_time)
                        };
                    }

                    foreach (var trayElem in traysElem.EnumerateArray())
                    {
                        if (amsElem.TryGetString(out var trayId, "id")
                            && trayElem.TryGetString(out var tray_type, "tray_type")
                            && trayElem.TryGetString(out var tray_color, "tray_color")
                            && trayElem.TryGetString(out var tray_info_idx, "tray_info_idx"))
                        {
                            var trayMaterial = new Material(tray_type, new MaterialColor(null, tray_color), tray_info_idx);
                            
                            materialUnit.Loaded.Add(trayMaterial);
                        }
                    }

                    bleh.Add(materialUnit);
                }

                data.MaterialUnits = bleh;
            }

            if (doGetVersion)
            {
                _ = this.PublishGetFirmwareVersion();
                Logger.TraceSuccess("doGetVersion required for AMS Mappings!");
            }


            // The A1 series only sends the data that's been changed. This mode can be determined by the "msg" property being set to 1.



            // State isn't included on the A1 Series

            // AMS ID 255 = External

            // Regular AMS'S begin at 129?

            // AMS ID, HT'S begin at 128?

            // "tray_info_idx" is the filament_id of the JSON filament profiles on BambuLab Studio.

            // length of ams[i].tray array is the # of slots.
            // each tray slot has a 

            // tray_type = MATERIAL, (ex, PLA, PETG)

            // tray_color (ex, 161616FF) (remove the last two FF's for hex)

            // "cols": [
            //"161616FF"
            //],
            //"ctype": 0,

            // Maybe related to solid colors or gradients? 

            // Use humidity_raw for percentage.

            // state = 11, loaded?
            // state = 10, nothing loaded?

            // dry_time is remaining drying time in minutes. We don't know the target temp nor the total drying time. Auggh.

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
                    }
                    break;
                }
            }
        }

        public static TimeSpan? CalculateTotalTime(double secondsRemaining, double percentCompleted)
        {
            if (percentCompleted >= 100) return null;

            if (percentCompleted < 0) return null;

            double percentRemaining = (100.0 - percentCompleted) / 100.0;

            if (percentRemaining <= 0) return null;

            return TimeSpan.FromSeconds(secondsRemaining / percentRemaining);
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

    internal struct BBLMQTTData
    {
        public bool? Connection;
        public MachineStatus? Status;
        public PartialMachinePrintJob? PrintJob;
        public BBLFirmwareVersion? FirmwareVersion;
        public HashSet<MaterialUnit> MaterialUnits;
        public bool? UsesUnsupportedSecurity;
        public MachineAirDuctMode? HVACMode;
        
    }
}