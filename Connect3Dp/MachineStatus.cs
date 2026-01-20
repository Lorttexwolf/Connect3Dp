namespace Connect3Dp
{
    [Flags]
    public enum MachineStatus
    {
        None = 0,
        Unknown = 1,
        /// <summary>
        /// Nothing is on a build-plate and machine is ready to print.
        /// </summary>
        Idle = 2,
        Printing = 4,
        Printed = 8,
        /// <summary>
        /// Printing or preparing has been paused and may be resumed.
        /// </summary>
        Paused = 32,
        /// <summary>
        /// Printing was aborted/canceled and model may be present.
        /// </summary>
        Stopped = 64,
        /// <summary>
        /// An exception was encountered while printing, review messages.
        /// </summary>
        Failed = 128
    }
}
