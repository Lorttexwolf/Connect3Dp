using Connect3Dp.Constants;
using Connect3Dp.Plugins.OME;
using Connect3Dp.SourceGeneration;
using Connect3Dp.Tracked;
using PartialSourceGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Connect3Dp
{
    [GenerateTracked, Partial]
    public partial class MachineState(string nickname, string uID, string company, string model) : IUniquelyIdentifiable
    {
        public string UID { get; internal set; } = uID;
        public string Nickname { get; internal set; } = nickname;
        public string Company { get; internal set; } = company;
        public string Model { get; internal set; } = model;

        public bool IsConnected { get; internal set; } = false;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MachineFeature Features { get; internal set; } = MachineFeature.None;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MachineStatus Status { get; internal set; } = MachineStatus.Unknown;

        public MachinePrintJob? CurrentJob { get; internal set; } = null;

        public HashSet<MaterialUnit> MaterialUnits { get; internal set; } = [];

        public HashSet<MachineMessage> Messages { get; internal set; } = [];

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MachineAirDuctMode? AirDuctMode { get; internal set; }

        public Dictionary<string, bool> LightFixtures { get; internal set; } = [];

        public string? StreamingOMEURL { get; internal set; }
        public string? ThumbnailOMEURL { get; internal set; }
    }

    [Partial]
    public partial class MachinePrintJob
    {
        public required string Name { get; set; }
        public string? FilePath { get; set; }
        public string? Stage { get; set; }
        public required int PercentageComplete { get; set; }
        public required TimeSpan RemainingTime { get; set; }
        public required TimeSpan TotalTime { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is MachinePrintJob job &&
                   Name == job.Name &&
                   FilePath == job.FilePath &&
                   Stage == job.Stage &&
                   PercentageComplete == job.PercentageComplete &&
                   RemainingTime.Equals(job.RemainingTime) &&
                   TotalTime.Equals(job.TotalTime);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, FilePath, Stage, PercentageComplete, RemainingTime, TotalTime);
        }
    }

    public partial class MachineState
    {
        /// <summary>
        /// Ensures the given <see cref="MachinePrintOptions"/> assigned properties are supported by the machine or else a <see cref="NotSupportedException"/> is thrown.
        /// </summary>
        internal void EnsureSupportPrintOptions(PrintOptions options)
        {
            if (options.FlowCalibration && !this.HasFeature(MachineFeature.Print_Options_FlowCalibration))
            {
                throw new NotSupportedException("Flow Calibration is not supported as a print option");
            }
            else if (options.VibrationCalibration && !this.HasFeature(MachineFeature.Print_Options_VibrationCalibration))
            {
                throw new NotSupportedException("Vibration Calibration is not supported as a print option");
            }
            else if (options.LevelBed && !this.HasFeature(MachineFeature.Print_Options_BedLevel))
            {
                throw new NotSupportedException("Bed Leveling is not supported as a print options");
            }
            else if (options.InspectFirstLayer && !this.HasFeature(MachineFeature.Print_Options_InspectFirstLayer))
            {
                throw new NotSupportedException("Inspect First Layer is not supported as a print option");
            }
        }

        public bool HasFeature(MachineFeature desiredFeature)
        {
            return (Features & desiredFeature) != MachineFeature.None;
        }

        /// <summary>
        /// <see cref="MachineException"/> is thrown when the current machine does not support the <paramref name="desiredFeature"/>
        /// </summary>
        /// <exception cref="MachineException"></exception>
        internal void EnsureHasFeature(MachineFeature desiredFeature)
        {
            if (!this.HasFeature(desiredFeature))
            {
                throw new MachineException(MachineMessages.NoFeature(desiredFeature));
            }
        }
    }
}
