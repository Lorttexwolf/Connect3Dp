using System.Text.Json.Serialization;

namespace Connect3Dp.State
{
    public interface IReadOnlyMachineState : IUniquelyIdentifiable
    {
        string Company { get; }
        string Model { get; }
        string? Nickname { get; }
        bool IsConnected { get; }
        [JsonConverter(typeof(JsonStringEnumConverter))] MachineCapabilities Capabilities { get; }
        [JsonConverter(typeof(JsonStringEnumConverter))] MachineStatus Status { get; }
        IReadOnlyMachinePrintJob? Job { get; }
        IEnumerable<HistoricPrintJob> JobHistory { get; }
        IEnumerable<MachineNozzle> Nozzles { get; }
        IEnumerable<IReadOnlyMaterialUnit> MaterialUnits { get; }
        IEnumerable<MachineMessage> Messages { get; }
        [JsonConverter(typeof(JsonStringEnumConverter))] MachineAirDuctMode? AirDuctMode { get; }
        IReadOnlyDictionary<string, bool> Lights { get; }
        IReadOnlyDictionary<string, int> Fans { get; }
        string? StreamingOMEURL { get; }
        string? ThumbnailOMEURL { get; }
    }
}
