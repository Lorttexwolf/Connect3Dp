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
        string SerialNumber,
        Material? LoadedMaterial = null
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

        // File-list pagination. The K1C pages file listings (typically 5 per page),
        // so we request pages sequentially and accumulate results across responses
        // before committing the full list to state.
        private const int DesiredFilePageSize = 100;
        private const int MaxFilePages = 200;
        private readonly object _paginationLock = new();
        private List<LocalPrintJob> _pendingFiles = new();
        private int _nextFilePage = 1;
        private int _firstPageSize = -1;
        private bool _paginatingFiles;
        private DateTime _paginationLastActivity = DateTime.MinValue;

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

                    // Register the framework-managed filament slot. Creality K1C does not report
                    // what material is loaded; the user tracks it here via ChangeMaterial.
                    update.UpdateMaterialUnits("External", mu => mu
                        .SetCapacity(1)
                        .SetCapabilities(MUCapabilities.ModifyTray));

                    if (Configuration.LoadedMaterial is { } mat)
                        update.UpdateMaterialUnits("External", mu => mu
                            .UpdateTrays(0, t => t.UpdateMaterial(m => m
                                .SetName(mat.Name)
                                .SetColor(mat.Color)
                                .SetFProfileIDX(mat.FProfileIDX))));

                    update.UpdateExtruders(0, e => e.SetLoadedSpool(new SpoolLocation("External", 0)));
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

            lock (_paginationLock)
            {
                _pendingFiles = new List<LocalPrintJob>();
                _nextFilePage = 1;
                _firstPageSize = -1;
                _paginatingFiles = false;
                _paginationLastActivity = DateTime.MinValue;
            }
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

        /// <summary>
        /// Periodic GET used to refresh file list and history. The K1C paginates file listings
        /// (typically 5 per page regardless of <c>onePageNum</c>), so this kicks off a fresh
        /// pagination cycle starting at page 1. Subsequent pages are requested from
        /// <see cref="HandleFilePage"/> as each response arrives.
        /// </summary>
        private Task SendGetFilesAndHistoryAsync()
        {
            bool startFresh;

            lock (_paginationLock)
            {
                // If a pagination cycle is active and fresh, don't disrupt it — just wait for completion.
                if (_paginatingFiles && (DateTime.UtcNow - _paginationLastActivity) < TimeSpan.FromSeconds(30))
                    return Task.CompletedTask;

                _pendingFiles = new List<LocalPrintJob>();
                _nextFilePage = 1;
                _firstPageSize = -1;
                _paginatingFiles = true;
                _paginationLastActivity = DateTime.UtcNow;
                startFresh = true;
            }

            return startFresh ? SendFileRequestAsync(1, includeHistory: true) : Task.CompletedTask;
        }

        /// <summary>Requests a single page of the paginated file list.</summary>
        private Task SendFileRequestAsync(int page, bool includeHistory)
        {
            if (includeHistory)
            {
                return SendJsonAsync(new
                {
                    method = "get",
                    @params = new
                    {
                        reqHistory = 1,
                        pFileList = page,
                        onePageNum = DesiredFilePageSize
                    }
                });
            }

            return SendJsonAsync(new
            {
                method = "get",
                @params = new
                {
                    pFileList = page,
                    onePageNum = DesiredFilePageSize
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

        private void MapTelemetryAndJob(JsonElement obj, MachineStateUpdate update)
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

            // Pass 1 — Status: only when the message includes a state field.
            // Sets MachineStatus and initialises the job name/path on the first active message.
            bool stateWasActive = false;
            if (obj.TryGetInt32Lenient("state", out var crealityState))
            {
                var mapped = CrealityK1CConstants.MapDeviceState(crealityState);
                update.SetStatus(mapped);

                var active = mapped is MachineStatus.Printing or MachineStatus.Paused;
                var ended  = mapped is MachineStatus.Printed  or MachineStatus.Canceled;

                if (active || ended)
                {
                    stateWasActive = true;
                    var path = obj.TryGetStringValue("printFileName", out var pfn) ? pfn
                             : obj.TryGetStringValue("filePath",      out var fp)  ? fp
                             : obj.TryGetStringValue("printPath",     out var pp)  ? pp : "";
                    var name = string.IsNullOrEmpty(path) ? "Unknown" : Path.GetFileNameWithoutExtension(path);

                    update.UpdateCurrentJob(job =>
                    {
                        job.SetName(name);
                        if (!string.IsNullOrEmpty(path)) job.SetLocalPath(path);
                        if (ended && mapped is MachineStatus.Printed) job.SetPercentageComplete(100);
                    });
                }
                else
                {
                    update.UnsetCurrentJob();
                }
            }

            // Pass 2 — Progress: runs on every message that carries progress/time/layer fields,
            // regardless of whether state was present. The K1C sends these in separate push
            // messages that do not include state, so they must not be gated on pass 1.
            var hasActiveJob = stateWasActive || State.CurrentJob != null;
            if (hasActiveJob)
            {
                var hasProgress  = obj.TryGetInt32Lenient("printProgress",  out var prog);
                var hasRemaining = obj.TryGetDurationLenient("printLeftTime", out var rem);
                var hasElapsed   = obj.TryGetDurationLenient("printJobTime",  out var el);

                string? subStage = null;
                if (obj.TryGetStringValue("layer", out var layerStr) && obj.TryGetStringValue("TotalLayer", out var totalStr))
                    subStage = $"Layer {layerStr}/{totalStr}";

                if (hasProgress || hasRemaining || hasElapsed || subStage != null)
                {
                    update.UpdateCurrentJob(job =>
                    {
                        if (hasProgress)  job.SetPercentageComplete(Math.Clamp(prog, 0, 100));
                        if (hasRemaining) job.SetRemainingTime(rem);

                        if (hasRemaining || hasElapsed)
                        {
                            var total = hasElapsed ? el : TimeSpan.Zero;
                            if (rem > TimeSpan.Zero && prog > 0 && prog < 100)
                                total = TimeSpan.FromSeconds(rem.TotalSeconds / (1.0 - prog / 100.0));
                            else if (rem > TimeSpan.Zero && hasElapsed && el > TimeSpan.Zero)
                                total = el + rem;
                            if (total > TimeSpan.Zero) job.SetTotalTime(total);
                        }

                        if (subStage != null) job.SetSubStage(subStage);
                    });
                }
            }
        }

        private void MapFiles(JsonElement obj, MachineStateUpdate update)
        {
            if (obj.TryGetProperty("pFileList", out var pFileList) && pFileList.ValueKind == JsonValueKind.Array)
            {
                HandleFilePage(pFileList, update);
                return;
            }

            if (!obj.TryGetProperty("retGcodeFileInfo", out var info) || info.ValueKind != JsonValueKind.Object)
                return;

            if (!info.TryGetProperty("fileInfo", out var fileInfo) || fileInfo.ValueKind != JsonValueKind.Array)
                return;

            // Non-paginated full listing fallback. Replace state in one shot.
            CommitFileList(ExtractFiles(fileInfo).ToList(), update);
        }

        /// <summary>
        /// Accumulates one page of paginated file results. When the page is empty or smaller than
        /// the first page received this cycle, commits the accumulated list and ends the cycle.
        /// Otherwise requests the next page.
        /// </summary>
        private void HandleFilePage(JsonElement pageArray, MachineStateUpdate update)
        {
            var pageFiles = ExtractFiles(pageArray).ToList();

            bool commitNow;
            int nextPageToRequest = 0;
            List<LocalPrintJob>? toCommit = null;

            lock (_paginationLock)
            {
                _pendingFiles.AddRange(pageFiles);
                _paginationLastActivity = DateTime.UtcNow;

                bool isEmpty = pageFiles.Count == 0;
                bool isShorterThanFirstPage = _firstPageSize > 0 && pageFiles.Count < _firstPageSize;
                bool hitMax = _nextFilePage >= MaxFilePages;

                if (_firstPageSize < 0 && pageFiles.Count > 0)
                    _firstPageSize = pageFiles.Count;

                if (isEmpty || isShorterThanFirstPage || hitMax)
                {
                    toCommit = _pendingFiles;
                    _pendingFiles = new List<LocalPrintJob>();
                    _nextFilePage = 1;
                    _firstPageSize = -1;
                    _paginatingFiles = false;
                    commitNow = true;
                }
                else
                {
                    _nextFilePage++;
                    nextPageToRequest = _nextFilePage;
                    commitNow = false;
                }
            }

            if (commitNow && toCommit != null)
                CommitFileList(toCommit, update);
            else if (nextPageToRequest > 0)
            {
                var page = nextPageToRequest;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (Socket?.State == WebSocketState.Open)
                            await SendFileRequestAsync(page, includeHistory: false);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Failed to request next file page ({page})");
                    }
                });
            }
        }

        private IEnumerable<LocalPrintJob> ExtractFiles(JsonElement array)
        {
            foreach (var file in array.EnumerateArray())
            {
                var path = file.TryGetStringValue("path", out var p) ? p : "";
                var name = file.TryGetStringValue("name", out var n) ? n : "";
                if (string.IsNullOrEmpty(path) && string.IsNullOrEmpty(name))
                    continue;

                if (string.IsNullOrEmpty(path))
                    path = $"{CrealityK1CConstants.GcodeDirectory}/{name}";

                long size = file.TryGetInt64Lenient("file_size", out var sz) ? sz : 0;

                var hash = Convert.ToHexStringLower(SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes($"{path}:{size}")));
                var handle = new MachineFileHandle(ID, path, "application/gcode", hash);
                var jobName = Path.GetFileNameWithoutExtension(string.IsNullOrEmpty(name) ? path : name);

                yield return new LocalPrintJob(
                    string.IsNullOrEmpty(jobName) ? "Unknown" : jobName,
                    handle, 0, TimeSpan.Zero,
                    new Dictionary<int, MaterialToPrint>());
            }
        }

        private void CommitFileList(List<LocalPrintJob> files, MachineStateUpdate update)
        {
            var seen = new HashSet<MachineFileHandle>();
            var deduped = new List<LocalPrintJob>();
            foreach (var job in files)
                if (seen.Add(job.File)) deduped.Add(job);

            foreach (var existing in State.LocalJobs)
                update.RemoveLocalJobs(existing);

            foreach (var job in deduped)
                update.SetLocalJobs(job);

            _hasLocalFiles = deduped.Count > 0;
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

        protected override Task<MachineOperationResult> Invoke_ChangeMaterial(SpoolLocation location, Material material)
        {
            CommitState(update =>
                update.UpdateMaterialUnits(location.MUID, mu =>
                    mu.UpdateTrays(location.Slot, t => t.UpdateMaterial(m => m
                        .SetName(material.Name)
                        .SetColor(material.Color)
                        .SetFProfileIDX(material.FProfileIDX)))));

            Configuration = Configuration with { LoadedMaterial = material };
            NotifyConfigurationChanged();

            return Task.FromResult(MachineOperationResult.Ok);
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
