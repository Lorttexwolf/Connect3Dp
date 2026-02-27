using Lib3Dp.State;

namespace Connect3Dp.Extensions.JeWebSocket
{
	public record struct AtAGlanceMachineStateChanges(
		bool StatusHasChanged,
		MachineStatus? StatusPrevious,
		MachineStatus? StatusNew,
		bool CapabilitiesHasChanged,
		MachineCapabilities? CapabilitiesPrevious,
		MachineCapabilities? CapabilitiesNew,
		bool NicknameHasChanged,
		string? NicknamePrevious,
		string? NicknameNew,
		PrintJobChanges? CurrentJobChanges)
	{
		public readonly bool HasChanged =>
			StatusHasChanged || CapabilitiesHasChanged || NicknameHasChanged || CurrentJobChanges?.HasChanged == true;

		public static AtAGlanceMachineStateChanges Of(in MachineStateChanges changes) => new(
			changes.StatusHasChanged, changes.StatusPrevious, changes.StatusNew,
			changes.CapabilitiesHasChanged, changes.CapabilitiesPrevious, changes.CapabilitiesNew,
			changes.NicknameHasChanged, changes.NicknamePrevious, changes.NicknameNew,
			changes.CurrentJobChanges);
	}
}
