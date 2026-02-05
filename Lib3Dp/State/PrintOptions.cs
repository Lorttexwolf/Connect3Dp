namespace Lib3Dp.State
{
	public record struct PrintOptions(bool LevelBed, bool FlowCalibration, bool VibrationCalibration, bool InspectFirstLayer, Dictionary<int, SpoolLocation>? MaterialMap);
}
