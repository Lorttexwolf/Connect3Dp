namespace Connect3Dp.State
{
    [Flags]
    public enum MaterialUnitFeatures
    {
        None = 0,
        AutomaticFeeding = 1 << 1,
        Heating = 1 << 2,
        Heating_TargetTemp = 1 << 3, // Bambu Lab doesn't support this as of 1/16/2026 :skull:
        Heating_CanSpin = 1 << 4,
        Heating_CanInUse = 1 << 5,
        Humidity = 1 << 6,
        Temperature = 1 << 7
    }
}
