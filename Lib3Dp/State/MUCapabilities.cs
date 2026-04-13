namespace Lib3Dp.State
{
	[Flags]
	public enum MUCapabilities
	{
		None = 0,
		AutomaticFeeding = 1 << 1,
		Heating = 1 << 2,
		Heating_CanSpin = 1 << 3,
		Humidity = 1 << 4,
		Temperature = 1 << 5,
		/// <summary>
		/// Users may modify the trays inside a MU.
		/// </summary>
		ModifyTray = 1 << 6,
		/// <summary>
		/// Users may not modify the maximum grams a tray can hold.
		/// </summary>
		ModifyTray_CannotMaxGrams = 1 << 7
	}
}
