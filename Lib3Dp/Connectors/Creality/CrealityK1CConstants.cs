using Lib3Dp.State;

namespace Lib3Dp.Connectors.Creality
{
    internal static class CrealityK1CConstants
    {
        public const string GcodeDirectory = "/usr/data/printer_data/gcodes";

        public const string LightFixtureChamber = "Chamber";

        public const string FanModel = "ModelFan";
        public const string FanAuxiliary = "AuxiliaryFan";
        public const string FanChamber = "BoxFan";

        /// <summary>Capabilities advertised once connected (must satisfy K1C ModelSpec and must not include ExplicitlyAbsent flags).</summary>
        public static readonly MachineCapabilities BaseCapabilities =
            MachineCapabilities.StartLocalJob |
            MachineCapabilities.Control |
            MachineCapabilities.Lighting |
            MachineCapabilities.LocalJobs |
            MachineCapabilities.Temps |
            MachineCapabilities.Fans |
            MachineCapabilities.PrintHistory;

        public static readonly HeatingConstraints BedConstraints = new(0, 100);
        public static readonly HeatingConstraints NozzleConstraints = new(0, 300);
        public static readonly HeatingConstraints ChamberConstraints = new(0, 60);

        /// <summary>Maps Creality LAN <c>state</c> (see Creality Print dashboard / Klipper bridge).</summary>
        public static MachineStatus MapDeviceState(int state) => state switch
        {
            0 => MachineStatus.Idle,
            1 => MachineStatus.Printing,
            2 => MachineStatus.Printed,
            3 => MachineStatus.Canceled,
            4 => MachineStatus.Canceled,
            5 => MachineStatus.Paused,
            _ => MachineStatus.Idle
        };
    }
}
