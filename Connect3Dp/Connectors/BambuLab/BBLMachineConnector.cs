using Connect3Dp.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Connect3Dp.Connectors.BambuLab
{
    public class BBLMachineConnector : MachineConnector
    {
        public BBLFirmwareVersion? FirmwareVersion { get; private set; }
        public IPAddress Address { get; }
        public string SerialNumber { get; }
        public string PrefixSerialNumber { get; }
        public string AccessCode { get; }

        private readonly BBLMQTTConnection MQTT;
        private bool PrevUsesUnsupportedSecurity { get; set; }

        private BBLMachineConnector(string nickname, string sn, string accessCode, IPAddress address) : base(nickname, sn, "BambuLab", BBLConstants.GetModelFromSerialNumber(sn))
        {
            this.Address = address;
            this.SerialNumber = sn;
            this.PrefixSerialNumber = sn[..3];
            this.AccessCode = accessCode;
            this.MQTT = new BBLMQTTConnection(address, sn, accessCode);

            this.MQTT.OnData += MQTT_OnData;
        }

        protected override async Task Connect_Internal(CancellationToken cancellationToken = default)
        {
            await this.MQTT.Connect();
        }

        private void MQTT_OnData(BBLMQTTData data)
        {
            if (data.Connection.HasValue)
            {
                this.State.IsConnected = data.Connection.Value;

                if (!data.Connection.Value)
                {
                    // Disconnected, reset security flag.
                    this.PrevUsesUnsupportedSecurity = false;
                }
            }

            if (data.FirmwareVersion.HasValue && !this.PrevUsesUnsupportedSecurity)
            {
                this.FirmwareVersion = data.FirmwareVersion.Value;

                MachineFeature machineFeatures = MachineFeature.Lighting | MachineFeature.Controllable | MachineFeature.Print;

                // We will decide which features are available on this machine depending on the model!

                // All models have automatic bed-leveling capability.
                machineFeatures |= MachineFeature.Print_Options_BedLevel;

                if (BBLConstants.ModelFeatures.WithInspectFirstLayer.Contains(this.State.Model))
                {
                    machineFeatures |= MachineFeature.Print_Options_InspectFirstLayer;
                }

                if (BBLConstants.ModelFeatures.WithFlowRateCali.Contains(this.State.Model))
                {
                    machineFeatures |= MachineFeature.Print_Options_FlowCalibration;
                }

                if (BBLConstants.ModelFeatures.WithClimateControl.Contains(this.State.Model))
                {
                    machineFeatures |= MachineFeature.AirDuct;
                }

                if (BBLConstants.ModelFeatures.WithRTSPSCamera.Contains(this.State.Model))
                {
                    machineFeatures |= MachineFeature.OME; // Currently (1/17/2026), only RTSPS is supported.
                }

                this.State.Features = machineFeatures;
            }

            if (data.UsesUnsupportedSecurity.HasValue && data.UsesUnsupportedSecurity.Value)
            {
                // Without LAN Mode and Developer Mode we cannot control anything.
                // When switching to these modes, the machine must be restarted.

                this.State.Features &= ~(MachineFeature.Print | MachineFeature.Controllable | MachineFeature.AirDuct);
            }

            if (data.PrintJob != null)
            {
                if (this.State.CurrentJob == null)
                {
                    if (data.PrintJob.Name != null 
                        && data.PrintJob.TotalTime.HasValue 
                        && data.PrintJob.RemainingTime.HasValue 
                        && data.PrintJob.PercentageComplete.HasValue
                        && data.PrintJob.Stage != null)
                    {
                        this.State.CurrentJob = new MachinePrintJob()
                        {
                            Name = data.PrintJob.Name,
                            PercentageComplete = data.PrintJob.PercentageComplete.Value,
                            RemainingTime = data.PrintJob.RemainingTime.Value,
                            Stage = data.PrintJob.Stage,
                            TotalTime = data.PrintJob.TotalTime.Value,
                            FilePath = data.PrintJob.FilePath
                        };
                    }
                    else
                    {
                        // Not all data exists, ask for a push of ALL data.
                        //_ = this.MQTT.PublishPushAll();

                    }
                }
                else
                {
                    this.State.CurrentJob.TotalTime = data.PrintJob.TotalTime.GetValueOrDefault(this.State.CurrentJob.TotalTime);
                    this.State.CurrentJob.RemainingTime = data.PrintJob.RemainingTime.GetValueOrDefault(this.State.CurrentJob.RemainingTime);
                    this.State.CurrentJob.Stage = data.PrintJob.Stage ?? this.State.CurrentJob.Stage;
                    this.State.CurrentJob.Name = data.PrintJob.Name ?? this.State.CurrentJob.Name;
                    this.State.CurrentJob.FilePath = data.PrintJob.FilePath ?? this.State.CurrentJob.FilePath;
                    this.State.CurrentJob.PercentageComplete = data.PrintJob.PercentageComplete.GetValueOrDefault(this.State.CurrentJob.PercentageComplete);
                }
            }

            if (data.Status.HasValue)
            {
                // Status has changed.
                this.State.Status = data.Status.Value;
            }

            if (data.HVACMode.HasValue)
            {
                this.State.AirDuctMode = data.HVACMode.Value;
            }

            if (data.MaterialUnits != null)
            {
                this.State.MaterialUnits = data.MaterialUnits;
            }

            _ = base.PollChanges();
        }

        internal override bool OvenMediaEnginePullURL_Internal([NotNullWhen(true)] out string? passURL)
        {
            if (BBLConstants.ModelFeatures.WithRTSPSCamera.Contains(this.State.Model))
            {
                // RTSPS
                passURL = $"rtsps://bblp:{AccessCode}@{Address}:322/streaming/live/1";
            }
            else
            {
                // TODO: A1 Mini, A1, P1P, P1S
                passURL = null;
            }
            return passURL!= null;
        }

        protected override Task ChangeAirDuctMode_Internal(MachineAirDuctMode mode)
        {
            return this.MQTT.PublishHVACModeCommand(mode);
        }

        public static BBLMachineConnector LAN(string nickname, string sn, string accessCode, IPAddress address)
        {
            return new BBLMachineConnector(nickname, sn, accessCode, address);
        }
    }
}
