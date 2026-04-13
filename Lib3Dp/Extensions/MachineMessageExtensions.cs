using Lib3Dp.State;
using Lib3Dp.Utilities;

namespace Lib3Dp.Extensions
{
	public static class MachineMessageExtensions
	{
		public static void Log(this Logger logger, MachineMessage message)
		{
			Logger.Level mappedSeverity = message.Severity switch
			{
				MachineMessageSeverity.Error => Logger.Level.Error,
				MachineMessageSeverity.Warning => Logger.Level.Warning,
				_ => Logger.Level.Trace
			};

			logger.Log(mappedSeverity, $"Message: {message.Title} - {message.Body}");
		}
	}
}
