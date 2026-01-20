using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Connect3Dp.Connectors.BambuLab
{
    internal static class BBLErrors
    {
        private static readonly HttpClient _httpClient;

        private static readonly ConcurrentDictionary<string, Lazy<Task<BBLMachineErrors>>> DownloadedErrors;


        //private static readonly PeriodicAsyncAction periodicCheckForUpdates();

        static BBLErrors()
        {
            _httpClient = new()
            {
                Timeout = TimeSpan.FromSeconds(30),
                BaseAddress = new Uri("https://e.bambulab.com/query.php")
            };
            DownloadedErrors = [];
        }

        internal static bool TryGetHMS(BBLMachineConnector machine, string ecode, [NotNullWhen(true)] out string? intro)
        {
            intro = null;

            if (!DownloadedErrors.TryGetValue(machine.PrefixSerialNumber, out var lazy)) return false;

            var task = lazy.Value;

            if (!task.IsCompletedSuccessfully) return false;

            return task.Result.HMS.TryGetValue(ecode, out intro);
        }

        internal static bool TryGetDevice(BBLMachineConnector machine, string ecode, [NotNullWhen(true)] out string? intro)
        {
            intro = null;

            if (!DownloadedErrors.TryGetValue(machine.PrefixSerialNumber, out var lazy)) return false;

            var task = lazy.Value;

            if (!task.IsCompletedSuccessfully) return false;

            return task.Result.Device.TryGetValue(ecode, out intro);
        }

        /// <summary>
        /// Downloads the HMS, and Device error codes from BBL if not downloaded already.
        /// </summary>
        internal static void DownloadIfRequired(BBLMachineConnector forMachine)
        {
            DownloadedErrors.GetOrAdd(
                forMachine.PrefixSerialNumber,
                sn => new Lazy<Task<BBLMachineErrors>>(
                    () => DownloadHMSAndDeviceErrorsAsync(sn, "en"),
                    LazyThreadSafetyMode.ExecutionAndPublication
                )
            );
        }

        private static async Task<BBLErrorVersions> CheckVersionsAsync(string snPrefix, string lang, CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.GetAsync($"?lang={lang}&d={snPrefix}", cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var jsonDoc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            var root = jsonDoc.RootElement;

            if (!root.TryGetProperty("data", out var data)) throw new JsonException("Missing 'data' property in response.");
            if (!data.TryGetProperty("device_hms", out var hmsData)) throw new JsonException("Missing 'device_hms' property in response.");
            if (!data.TryGetProperty("device_error", out var deviceData)) throw new JsonException("Missing 'device_error' property in response.");

            var hmsVersion = GetVersion(hmsData, "HMS");
            var deviceVersion = GetVersion(deviceData, "device error");

            return new BBLErrorVersions(hmsVersion, deviceVersion);
        }

        /// <summary>
        /// Downloads HMS (Health Management System) and device error definitions from BambuLab.
        /// </summary>
        private static async Task<BBLMachineErrors> DownloadHMSAndDeviceErrorsAsync(string snPrefix, string lang, CancellationToken cancellationToken = default)
        {
            using var res = await _httpClient.GetAsync($"?lang={lang}&d={snPrefix}", cancellationToken).ConfigureAwait(false);
            res.EnsureSuccessStatusCode();

            await using var stream = await res.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var jsonDoc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            return ParseErrorData(jsonDoc, lang);
        }

        private static BBLMachineErrors ParseErrorData(JsonDocument jsonDoc, string lang)
        {
            var root = jsonDoc.RootElement;

            if (!root.TryGetProperty("data", out var data)) throw new JsonException("Missing 'data' property in response.");
            if (!data.TryGetProperty("device_hms", out var hmsData)) throw new JsonException("Missing 'device_hms' property in response.");
            if (!data.TryGetProperty("device_error", out var deviceData)) throw new JsonException("Missing 'device_error' property in response.");

            var hmsVersion = GetVersion(hmsData, "HMS");
            var deviceVersion = GetVersion(deviceData, "device error");

            var hmsErrors = ParseErrors(hmsData, lang, "HMS");
            var deviceErrors = ParseErrors(deviceData, lang, "device error");

            return new BBLMachineErrors(hmsErrors, deviceErrors, new BBLErrorVersions(hmsVersion, deviceVersion));
        }

        private static int GetVersion(JsonElement element, string errorType)
        {
            if (!element.TryGetProperty("ver", out var verElement)) throw new JsonException($"Missing 'ver' property in {errorType} data.");

            return verElement.GetInt32();
        }

        private static IReadOnlyDictionary<string, string> ParseErrors(JsonElement errorData, string lang, string errorType)
        {
            if (!errorData.TryGetProperty(lang, out var langData)) throw new JsonException($"Missing language '{lang}' in {errorType} data.");

            var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var error in langData.EnumerateArray())
            {
                if (!error.TryGetProperty("ecode", out var ecodeElement) || !error.TryGetProperty("intro", out var introElement))
                {
                    continue;
                }

                var ecode = ecodeElement.GetString();
                var intro = introElement.GetString();

                if (string.IsNullOrEmpty(ecode) || string.IsNullOrEmpty(intro)) continue;

                intro = intro.ReplaceLineEndings(" ");

                errors.Add(ecode, intro);
            }

            return errors;
        }
    }

    internal sealed record BBLMachineErrors(IReadOnlyDictionary<string, string> HMS, IReadOnlyDictionary<string, string> Device, BBLErrorVersions Versions);

    internal sealed record BBLErrorVersions(int HMSVersion, int DeviceVersion)
    {
        public bool HasUpdates(int currentHMSVersion, int currentDeviceVersion)
        {
            return HMSVersion > currentHMSVersion || DeviceVersion > currentDeviceVersion;
        }
    }
}