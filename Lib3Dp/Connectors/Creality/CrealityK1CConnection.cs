using Lib3Dp.Configuration;
using Lib3Dp.Constants;
using Lib3Dp.Extensions;
using Lib3Dp.Files;
using Lib3Dp.State;
using Lib3Dp.Utilities;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lib3Dp.Connectors.Creality
{
    public record CrealityK1CConfiguration(
        string? Nickname,
        string Address,
        string SerialNumber
    );

    public class CrealityK1CConnection : MachineConnection, IConfigurableConnection<CrealityK1CConnection, CrealityK1CConfiguration>
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly Logger Log;
        private SimpleWebSocketClient? Socket;
        private Timer? PollTimer;
        private string? _LastHistoryFingerprint;
        private bool _hasLocalFiles;

        public CrealityK1CConfiguration Configuration { get; private set; }

        public CrealityK1CConnection(IMachineFileStore fileStore, CrealityK1CConfiguration configuration) : base(
            fileStore,
            new MachineConnectionConfiguration(
                Nickname: configuration.Nickname,
                ID: configuration.SerialNumber,
                Brand: "Creality",
                Model: "K1C"))
        {
            Configuration = configuration;
            Log = Logger.OfCategory($"{nameof(CrealityK1CConnection)} ({configuration.SerialNumber ?? configuration.Nickname})");
        }

        #region Configuration

        public override CrealityK1CConfiguration GetConfiguration() => Configuration;

        public async Task<MachineOperationResult> UpdateConfiguration(CrealityK1CConfiguration updatedCfg)
        {
            var opResult = await Mono.MutateUntil(async () =>
            {
                await DisconnectSocket();
                Configuration = updatedCfg;
                await Connect_Internal();
            }, () => State.Status is not MachineStatus.Disconnected, TimeSpan.FromSeconds(30));

            return opResult.IntoOperationResult("update_cfg_fail", "Update Configuration");
        }

        public static Type GetConfigurationType() => typeof(CrealityK1CConfiguration);

        public static CrealityK1CConnection CreateFromConfiguration(IMachineFileStore fileStore, CrealityK1CConfiguration configuration)
        {
            return new CrealityK1CConnection(fileStore, configuration);
        }

        #endregion

        #region Connect / Disconnect

        protected override async Task<MachineOperationResult> Connect_Internal()
        {
            try
            {
                var host = NormalizePrinterHost(Configuration.Address);
                var wsUri = new Uri($"ws://{host}:9999");

                Socket = new SimpleWebSocketClient(wsUri);
                Socket.OnMessage += OnWebSocketMessage;
                Socket.OnDisconnected += OnWebSocketDisconnected;

                await Socket.ConnectAsync();

                Log.Info($"WebSocket connected to {wsUri}");

                // Optimistically assume storage is present until the first file list response
                _hasLocalFiles = true;

                CommitState(update =>
                {
                    update.SetStatus(MachineStatus.Idle);
                    update.SetCapabilities(CrealityK1CConstants.BaseCapabilities);
                    update.SetLights(CrealityK1CConstants.LightFixtureChamber, false);
                });

                await SendGetRefreshAsync();
                await SendGetFilesAndHistoryAsync();

                PollTimer = new Timer(async _ =>
                {
                    try
                    {
                        if (Socket?.State == WebSocketState.Open)
                            await SendGetFilesAndHistoryAsync();
                    }
                    catch { /* next poll */ }
                }, null, TimeSpan.FromSeconds(45), TimeSpan.FromSeconds(45));

                return MachineOperationResult.Ok;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to connect to Creality printer at {NormalizePrinterHost(Configuration.Address)}");

                return MachineOperationResult.Fail(
                    "creality.k1c.connect.failed",
                    "Unable to Connect",
                    ex.Message,
                    MachineMessageActions.CheckConfiguration,
                    new MachineMessageAutoResole { WhenConnected = true });
            }
        }

        public override async Task Disconnect()
        {
            await DisconnectSocket();

            CommitState(update =>
            {
                update.SetStatus(MachineStatus.Disconnected);
                update.SetCapabilities(MachineCapabilities.None);
                update.UnsetCurrentJob();
            });
        }

        private async Task DisconnectSocket()
        {
            PollTimer?.Dispose();
            PollTimer = null;

            if (Socket != null)
            {
                Socket.OnMessage -= OnWebSocketMessage;
                Socket.OnDisconnected -= OnWebSocketDisconnected;

                try { await Socket.DisposeAsync(); }
                catch { }

                Socket = null;
            }

            _LastHistoryFingerprint = null;
            _hasLocalFiles = false;
        }

        private void OnWebSocketDisconnected(WebSocketCloseStatus? status, string? description)
        {
            Log.Info($"WebSocket disconnected: {status} — {description}");

            CommitState(update =>
            {
                update.SetStatus(MachineStatus.Disconnected);
                update.SetCapabilities(MachineCapabilities.None);
                update.UnsetCurrentJob();
            });
        }

        #endregion

        #region WebSocket Protocol

        private async Task SendJsonAsync(object payload)
        {
            if (Socket == null || Socket.State != WebSocketState.Open)
                throw new InvalidOperationException("WebSocket is not connected");

            var json = JsonSerializer.Serialize(payload, JsonOptions);
            await Socket.SendTextAsync(json);
        }

        private Task SendSetAsync(object parameters)
        {
            return SendJsonAsync(new { method = "set", @params = parameters });
        }

        /// <summary>Periodic GET used to refresh file list and history (printer also pushes live telemetry).</summary>
        private Task SendGetFilesAndHistoryAsync()
        {
            return SendJsonAsync(new
            {
                method = "get",
                @params = new
                {
                    reqGcodeFile = 1,
                    reqHistory = 1,
                    pFileList = 1,
                    page_num = 1,
                    page_size = 100
                }
            });
        }

        /// <summary>GET that may prompt a fuller snapshot on some firmware builds.</summary>
        private Task SendGetRefreshAsync()
        {
            return SendJsonAsync(new
            {
                method = "get",
                @params = new
                {
                    reqGcodeFile = 1,
                    reqHistory = 1,
                    boxConfig = 1,
                    reqPrintObjects = 1
                }
            });
        }

        #endregion

        #region Incoming JSON

        private void OnWebSocketMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            try
            {
                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    return;

                var update = new MachineStateUpdate();

                MergePrinterState(root, update);

                if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                    MergePrinterState(data, update);

                update.SetCapabilities(ComputeCapabilities());
                ApplyStorageNotification(update);
                CommitState(update);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to parse Creality WebSocket message");
            }
        }

        private void MergePrinterState(JsonElement obj, MachineStateUpdate update)
        {
            MapTelemetryAndJob(obj, update);
            MapFiles(obj, update);
            MapHistory(obj, update);

            if (obj.TryGetStringValue("hostname", out var host) && !string.IsNullOrEmpty(host))
                update.SetNickname(host);
        }

        private static void MapTelemetryAndJob(JsonElement obj, MachineStateUpdate update)
        {
            if (obj.TryGetDoubleLenient("nozzleTemp", out var nozzleCur))
            {
                var target = obj.TryGetDoubleLenient("targetNozzleTemp", out var nt) ? nt : 0;
                update.SetHeatingElements(HeatingElementNames.Nozzle,
                    new HeatingElement(nozzleCur, target, CrealityK1CConstants.NozzleConstraints));
            }

            if (obj.TryGetDoubleLenient("bedTemp0", out var bedCur))
            {
                var target = obj.TryGetDoubleLenient("targetBedTemp0", out var bt) ? bt : 0;
                update.SetHeatingElements(HeatingElementNames.Bed,
                    new HeatingElement(bedCur, target, CrealityK1CConstants.BedConstraints));
            }

            if (obj.TryGetDoubleLenient("boxTemp", out var boxCur))
            {
                var target = obj.TryGetDoubleLenient("targetBoxTemp", out var tt) ? tt
                    : obj.TryGetDoubleLenient("targetBoxTemp0", out var tt0) ? tt0 : 0;
                update.SetHeatingElements(HeatingElementNames.Chamber,
                    new HeatingElement(boxCur, target, CrealityK1CConstants.ChamberConstraints));
            }

            if (obj.TryGetInt32Lenient("modelFanPct", out var modelPct))
                update.SetFans(CrealityK1CConstants.FanModel, Math.Clamp(modelPct, 0, 100));
            if (obj.TryGetInt32Lenient("auxiliaryFanPct", out var auxPct))
                update.SetFans(CrealityK1CConstants.FanAuxiliary, Math.Clamp(auxPct, 0, 100));
            if (obj.TryGetInt32Lenient("caseFanPct", out var casePct))
                update.SetFans(CrealityK1CConstants.FanChamber, Math.Clamp(casePct, 0, 100));

            if (obj.TryGetInt32Lenient("lightSw", out var light))
                update.SetLights(CrealityK1CConstants.LightFixtureChamber, light != 0);

            if (!obj.TryGetInt32Lenient("state", out var crealityState))
                return;

            var mapped = CrealityK1CConstants.MapDeviceState(crealityState);
            update.SetStatus(mapped);

            var active = mapped is MachineStatus.Printing or MachineStatus.Paused;
            var ended = mapped is MachineStatus.Printed or MachineStatus.Canceled;

            if (active || ended)
            {
                var path = obj.TryGetStringValue("printFileName", out var pfn) ? pfn
                    : obj.TryGetStringValue("filePath", out var fp) ? fp
                    : obj.TryGetStringValue("printPath", out var pp) ? pp : "";

                var name = string.IsNullOrEmpty(path) ? "Unknown" : Path.GetFileNameWithoutExtension(path);

                var progress = obj.TryGetInt32Lenient("printProgress", out var prog) ? Math.Clamp(prog, 0, 100) : 0;
                var remaining = obj.TryGetDurationLenient("printLeftTime", out var rem) ? rem : TimeSpan.Zero;
                var elapsed = obj.TryGetDurationLenient("printJobTime", out var el) ? el : TimeSpan.Zero;

                if (ended && mapped is MachineStatus.Printed)
                    progress = 100;

                var totalTime = elapsed;
                if (remaining > TimeSpan.Zero && progress > 0 && progress < 100)
                    totalTime = TimeSpan.FromSeconds(remaining.TotalSeconds / (1.0 - progress / 100.0));
                else if (remaining > TimeSpan.Zero && elapsed > TimeSpan.Zero)
                    totalTime = elapsed + remaining;

                string? subStage = null;
                if (obj.TryGetStringValue("layer", out var layerStr) && obj.TryGetStringValue("TotalLayer", out var totalStr))
                    subStage = $"Layer {layerStr}/{totalStr}";

                update.UpdateCurrentJob(job =>
                {
                    job.SetName(name);
                    job.SetPercentageComplete(progress);
                    job.SetRemainingTime(remaining);
                    job.SetTotalTime(totalTime);
                    if (!string.IsNullOrEmpty(path)) job.SetLocalPath(path);
                    if (subStage != null) job.SetSubStage(subStage);
                });
            }
            else
            {
                update.UnsetCurrentJob();
            }
        }

        private void MapFiles(JsonElement obj, MachineStateUpdate update)
        {
            if (obj.TryGetProperty("pFileList", out var pFileList) && pFileList.ValueKind == JsonValueKind.Array)
            {
                IngestFileArray(pFileList, update);
                return;
            }

            if (!obj.TryGetProperty("retGcodeFileInfo", out var info) || info.ValueKind != JsonValueKind.Object)
                return;

            if (!info.TryGetProperty("fileInfo", out var fileInfo) || fileInfo.ValueKind != JsonValueKind.Array)
                return;

            IngestFileArray(fileInfo, update);
        }

        private void IngestFileArray(JsonElement array, MachineStateUpdate update)
        {
            foreach (var existing in State.LocalJobs)
                update.RemoveLocalJobs(existing);

            int fileCount = 0;

            foreach (var file in array.EnumerateArray())
            {
                var path = file.TryGetStringValue("path", out var p) ? p : "";
                var name = file.TryGetStringValue("name", out var n) ? n : "";
                if (string.IsNullOrEmpty(path) && string.IsNullOrEmpty(name))
                    continue;

                if (string.IsNullOrEmpty(path))
                    path = $"{CrealityK1CConstants.GcodeDirectory}/{name}";

                long size = file.TryGetInt64Lenient("file_size", out var sz) ? sz : 0;

                var hash = Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{path}:{size}")));
                var uri = path;
                var handle = new MachineFileHandle(ID, uri, "application/gcode", hash);
                var jobName = Path.GetFileNameWithoutExtension(string.IsNullOrEmpty(name) ? path : name);

                update.SetLocalJobs(new LocalPrintJob(
                    string.IsNullOrEmpty(jobName) ? "Unknown" : jobName,
                    handle,
                    0,
                    TimeSpan.Zero,
                    new Dictionary<int, MaterialToPrint>()));

                fileCount++;
            }

            _hasLocalFiles = fileCount > 0;
        }

        private void MapHistory(JsonElement obj, MachineStateUpdate update)
        {
            if (!obj.TryGetProperty("historyList", out var list) || list.ValueKind != JsonValueKind.Array)
                return;

            var fingerprint = $"{list.GetArrayLength()}:{string.Join(",", list.EnumerateArray().Take(5).Select(e => e.TryGetInt32Lenient("id", out var id) ? id : 0))}";
            if (fingerprint == _LastHistoryFingerprint)
                return;
            _LastHistoryFingerprint = fingerprint;

            foreach (var entry in list.EnumerateArray())
            {
                var path = entry.TryGetStringValue("filename", out var fn) ? fn : "";
                var name = string.IsNullOrEmpty(path) ? "Unknown" : Path.GetFileNameWithoutExtension(path);
                var ok = entry.TryGetInt32Lenient("printfinish", out var pf) && pf == 1;
                var totalSec = entry.TryGetInt32Lenient("totaltime", out var tt) ? tt : 0;
                var usageSec = entry.TryGetInt32Lenient("usagetime", out var ut) ? ut : totalSec;

                var endUnix = entry.TryGetInt64Lenient("starttime", out var st) ? st + usageSec : 0;
                var ended = endUnix > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(endUnix).LocalDateTime
                    : DateTime.Now;

                update.SetJobHistory(new HistoricPrintJob(
                    name,
                    ok,
                    ended,
                    TimeSpan.FromSeconds(Math.Max(0, usageSec)),
                    null,
                    null));
            }
        }

        #endregion

        #region Capability & Storage Tracking

        private MachineCapabilities ComputeCapabilities()
        {
            var caps = CrealityK1CConstants.BaseCapabilities;

            if (!_hasLocalFiles)
                caps &= ~(MachineCapabilities.StartLocalJob | MachineCapabilities.LocalJobs);

            return caps;
        }

        private void ApplyStorageNotification(MachineStateUpdate update)
        {
            bool hasMissingNotification = State.MappedNotifications.ContainsKey(MachineMessages.SDCardOrUSBMissing.Id);

            if (!_hasLocalFiles && !hasMissingNotification)
                update.SetNotifications(MachineMessages.SDCardOrUSBMissing);
            else if (_hasLocalFiles && hasMissingNotification)
                update.RemoveNotifications(MachineMessages.SDCardOrUSBMissing.Id);
        }

        #endregion

        #region MachineConnection Overrides

        protected override Task DownloadLocalFile(MachineFileHandle fileHandle, Stream destinationStream)
        {
            throw new NotSupportedException(
                "Creality K1C LAN connector does not implement G-code download. Files are listed and printed by path only.");
        }

        protected override async Task PrintLocal_Internal(LocalPrintJob localPrint, PrintOptions options)
        {
            var path = NormalizeGcodePath(localPrint.File.URI);
            var op = $"printprt:{path}";

            await SendSetAsync(new { opGcodeFile = op, enableSelfTest = 0 });
        }

        protected override Task Pause_Internal()
        {
            return SendSetAsync(new { pause = 1 });
        }

        protected override Task Resume_Internal()
        {
            return SendSetAsync(new { pause = 0 });
        }

        protected override Task Stop_Internal()
        {
            return SendSetAsync(new { stop = 1 });
        }

        protected override Task ClearBed_Internal()
        {
            // Pragmatic exception: K1C firmware has no "clear bed" / "acknowledge complete" command.
            // Without this, MutateUntil in MarkAsIdle() always times out because no WebSocket message
            // will transition the printer to Idle after Printed/Canceled. ELEGOO uses the same pattern.
            CommitState(update =>
            {
                update.SetStatus(MachineStatus.Idle);
                update.UnsetCurrentJob();
            });
            return Task.CompletedTask;
        }

        protected override async Task ToggleLight_Internal(string fixtureName, bool isOn)
        {
            await SendSetAsync(new { lightSw = isOn ? 1 : 0 });

            // Pragmatic exception: firmware doesn't push light state quickly enough for MutateUntil.
            // Apply optimistically so the base class ToggleLight predicate resolves. Same as ELEGOO.
            if (fixtureName == CrealityK1CConstants.LightFixtureChamber)
                CommitState(u => u.SetLights(CrealityK1CConstants.LightFixtureChamber, isOn));
        }

        #endregion

        #region Paths

        private static string NormalizeGcodePath(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
                throw new InvalidOperationException("G-code path is empty.");

            var s = uri.Trim();
            if (s.StartsWith("printprt:", StringComparison.OrdinalIgnoreCase))
                s = s["printprt:".Length..].TrimStart();

            if (s.StartsWith('/'))
                return s;

            return $"{CrealityK1CConstants.GcodeDirectory}/{s.TrimStart('/')}";
        }

        /// <summary>Host only for <c>ws://…:9999</c>. Strips schemes, paths, and trailing <c>:9999</c> if duplicated.</summary>
        private static string NormalizePrinterHost(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return raw ?? string.Empty;

            var s = raw.Trim();

            if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                s = s["http://".Length..];
            else if (s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                s = s["https://".Length..];
            else if (s.StartsWith("ws://", StringComparison.OrdinalIgnoreCase))
                s = s["ws://".Length..];
            else if (s.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
                s = s["wss://".Length..];

            var pathSlash = s.IndexOf('/');
            if (pathSlash >= 0)
                s = s[..pathSlash];

            s = s.Trim();

            if (s.EndsWith(":9999", StringComparison.Ordinal))
                s = s[..^":9999".Length];

            return s.Trim();
        }

        #endregion
    }
}
