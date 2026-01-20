namespace Connect3Dp
{
    [Flags]
    public enum MachineFeature
    {
        None = 1 << 0,
        /// <summary>
        /// Prints (such as GCODE &amp; 3MF) can be sent and started remotely.
        /// </summary>
        Print = 1 << 1,
        /// <summary>
        /// Machine can be paused, resumed, and stopped remotely.
        /// </summary>
        Controllable = 1 << 2,
        /// <summary>
        /// Machine supports the ability to post it's camera stream, along with thumbnails.
        /// </summary>
        OME = 1 << 11,
        /// <summary>
        /// Machine supports toggling a light fixture.
        /// </summary>
        Lighting = 1 << 4,
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
