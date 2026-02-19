namespace Lib3Dp.State
{
	[Flags]
	public enum MachineStatus
	{
		Disconnected = 0,
		/// <summary>
		/// Nothing is on a build-plate and machine is ready to print.
		/// </summary>
		Idle = 1 << 0,
		/// <summary>
		/// Printing is in action.
		/// </summary>
		Printing = 1 << 1,
		/// <summary>
		/// Printing has been completed and the final result is on the build-plate.
		/// </summary>
		Printed = 1 << 2,
		/// <summary>
		/// Printing or preparing has been paused and may be resumed.
		/// </summary>
		Paused = 1 << 3,
		/// <summary>
		/// Printing was aborted/canceled and the finished model may not be present.
		/// </summary>
		Canceled = 1 << 4
	}
}
