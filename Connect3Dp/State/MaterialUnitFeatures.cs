namespace Connect3Dp.State
{
    [Flags]
    public enum MaterialUnitFeatures
    {
        None = 0,
        AutomaticFeeding = 1 << 1,
        Heating = 1 << 2,
        Heating_CanSpin = 1 << 3,
        Humidity = 1 << 4,
        Temperature = 1 << 5
    }
}
