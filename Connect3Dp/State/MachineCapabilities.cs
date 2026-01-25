using Connect3Dp.Connectors;

namespace Connect3Dp.State
{
    [Flags]
    public enum MachineCapabilities
    {
        None = 1 << 0,
        /// <summary>
        /// Prints (such as GCODE & 3MF) can be sent and started remotely.
        /// </summary>
        SendJob = 1 << 18,
        /// <summary>
        /// <see cref="LocalPrintJob">LocalPrintJobs</see> in <see cref="MachineState.LocalJobs"/> can be started remotely.
        /// </summary>
        StartLocalJob = 1 << 1,
        /// <summary>
        /// Machine can be paused, resumed, and stopped remotely.
        /// </summary>
        Control = 1 << 2,
        /// <summary>
        /// Machine supports the ability to post it's camera stream, along with thumbnails.
        /// </summary>
        OME = 1 << 12,
        /// <summary>
        /// Machine supports toggling a light fixture.
        /// </summary>
        Lighting = 1 << 4,
        /// <summary>
        /// Connector will populate <see cref="MachineState.JobHistory"/> with <see cref="HistoricPrintJob"/> and update as prints are completed.
        /// </summary>
        PrintHistory = 1 << 14,
        /// <summary>
        /// Enables <see cref="MachineFile"/> associated with the given <see cref="MachineConnector"/> to execute <see cref="MachineFile.Download(Stream)"/>.
        /// </summary>
        FetchFiles = 1 << 16,
        /// <summary>
        /// Advertises the capability of reading the <see cref="LocalPrintJob"/> which are located on the given machine.
        /// </summary>
        LocalJobs = 1 << 17,
        /// <summary>
        /// Fans
        /// </summary>
        Fans = 1 << 11,
        /// <summary>
        /// Machine supports reading nozzle information.
        /// </summary>
        Nozzles = 1 << 13,
        /// <summary>
        /// Machine supports the option to toggle heating, or cooling of the chamber.
        /// </summary>
        AirDuct = 1 << 5,
        /// <summary>
        /// Machine supports print option BED_LEVEL
        /// </summary>
        Print_Options_BedLevel = 1 << 7,
        /// <summary>
        /// Machine supports print option FLOW_CALIBRATION
        /// </summary>
        Print_Options_FlowCalibration = 1 << 8,
        /// <summary>
        /// Machine supports print option VIBRATION_CALIBRATION
        /// </summary>
        Print_Options_VibrationCalibration = 1 << 9,
        /// <summary>
        /// Machine supports print option INSPECT_FIRST_LAYER
        /// </summary>
        Print_Options_InspectFirstLayer = 1 << 10,
    }
}
