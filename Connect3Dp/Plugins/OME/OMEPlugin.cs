using Connect3Dp.Connectors;
using Connect3Dp.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Connect3Dp.Plugins.OME
{
    public class OMEPlugin : IPlugin<OMEPlugin, OMEPluginConnectorData>
    {
        private static readonly Logger Logger;
        public static readonly OMEPlugin? Instance;

        private readonly HttpClient RestAPI;
        private readonly Dictionary<string, MachineConnector> StreamsToMachine;

        private readonly PeriodicAsyncAction PeriodicRunner;

        private OMEPlugin(OMEPluginEnvironment environment)
        {
            this.RestAPI = MakeOMEHttpClient(environment);
            this.StreamsToMachine = [];
            this.PeriodicRunner = new PeriodicAsyncAction(TimeSpan.FromSeconds(30), Periodic);
        }

        internal async Task Periodic()
        {
            string[] liveStreams;

            try
            {
                liveStreams = await DoFetchStreams();
            }
            catch (HttpRequestException reqEx)
            {
                if (reqEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Logger.Error($"Not authorized to access the OME Stream API, check your Access Token.");
                }
                else
                {
                    Logger.Error($"Unable to Fetch OME Streams.\n{reqEx}");
                }
                return;
            }

            var machineLiveStreams = StreamsToMachine.Where(p => p.Value.State.HasFeature(MachineFeature.OME)).Select(p => p.Key);
            var missingStreams = machineLiveStreams.Except(liveStreams);

            foreach (var streamToAdd in missingStreams)
            {
                if (this.StreamsToMachine.TryGetValue(streamToAdd, out var machineConnector) && machineConnector.OvenMediaEnginePullURL_Internal(out var passURL))
                {
                    var streamName = MakeStreamName(machineConnector);

                    try
                    {
                        await DoCreateStreamPULL(streamName, passURL);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to add missing Stream: {streamName}\n{ex}");
                    }
                }
            }
        }

        private async Task<string[]> DoFetchStreams()
        {
            // https://docs.ovenmediaengine.com/rest-api/v1/virtualhost/application/stream#get-stream-list

            var response = await RestAPI.GetAsync("/v1/vhosts/default/apps/app/streams");
            response.EnsureSuccessStatusCode();

            var JSON = JsonDocument.Parse(response.Content.ReadAsStream());

            return [..JSON.RootElement.GetProperty("response").EnumerateArray().Select(e => e.GetString())!];
        }

        private async Task DoCreateStreamPULL(string streamName, string pullURL)
        {
            // https://docs.ovenmediaengine.com/rest-api/v1/virtualhost/application/stream#create-stream-pull

            using var content = JsonContent.Create(new
            {
                name = streamName,
                urls = new string[] { pullURL },
                properties = new
                {
                    persistent = false
                }
            });

            await RestAPI.PostAsync("/v1/vhosts/default/apps/app/streams", content);

            Logger.Trace($"Created OME stream named {streamName} ingesting {pullURL}");
        }

        static OMEPlugin()
        {
            Logger = Logger.Category(nameof(OMEPlugin));

            if (!TryGetEnvironment(out var env))
            {
                return;
            }

            Instance = new OMEPlugin(env);
        }

        public static bool TryGetEnvironment([NotNullWhen(true)] out OMEPluginEnvironment? environment)
        {
            var hostIP = Environment.GetEnvironmentVariable("OME_HOST_IP");
            var accessToken = Environment.GetEnvironmentVariable("OME_ACCESS_TOKEN");

            if (hostIP == null || accessToken == null)
            {
                environment = null;
                return false;
            }

            environment = new OMEPluginEnvironment(hostIP, accessToken);
            return true;
        }

        private static HttpClient MakeOMEHttpClient(OMEPluginEnvironment environment)
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(environment.HostIP),
            };

            var tokenBytes = Encoding.UTF8.GetBytes(environment.AccessToken);
            var base64Token = Convert.ToBase64String(tokenBytes);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Token);

            return client;
        }

        private static string MakeStreamName(MachineConnector forMachine)
        {
            return $"machine/{forMachine.State.UID}";
        }

        public static bool TryGetInstance([NotNullWhen(true)] out OMEPlugin? instance)
        {
            instance = Instance;
            return Instance != null;
        }

        public bool RegisterConnector(MachineConnector connector)
        {
            StreamsToMachine[MakeStreamName(connector)] = connector;
            return true;
        }

        public bool IsConnectorRegistered(MachineConnector connector)
        {
            return StreamsToMachine.ContainsValue(connector);
        }

        public OMEPluginConnectorData GetConnectorPluginData(MachineConnector connector)
        {
            // This will return the machine data associated with this plugin.

            return new OMEPluginConnectorData(MakeStreamName(connector), ThumbnailURL(MakeStreamName(connector)));
        }

        public static string ThumbnailURL(string streamName)
        {
            // https://docs.ovenmediaengine.com/0.17.3/thumbnail#get-thumbnails
            return $":20081/app/{streamName}/thumb.jpg";
        }
    }

    public record OMEPluginConnectorData(string OMEStream, string ThumbnailURL);

    public record OMEPluginEnvironment(string HostIP, string AccessToken);
}
