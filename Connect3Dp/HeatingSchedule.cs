namespace Connect3Dp
{
    public record HeatingSchedule(TimeOnly Begin, HeatingSettings Settings, int? OnlyAboveHumditiyPercent);
}
