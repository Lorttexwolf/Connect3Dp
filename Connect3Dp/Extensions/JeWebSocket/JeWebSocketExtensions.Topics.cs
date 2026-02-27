namespace Connect3Dp.Extensions.JeWebSocket
{
	public static partial class JeWebSocketExtensions
	{
		public static class Topics
		{
			public static string StateUpdated(string machineID) => $"machine/{machineID}/state";
			public static string ConfigurationUpdated(string machineID) => $"machine/{machineID}/configuration";

			public static class Logging
			{
				public const string Subscribe = "log/subscribe";
				public const string Unsubscribe = "log/unsubscribe";
				public const string History = "log/history";
				public const string Logs = "logs";
			}

			public static class Machine
			{
				public const string Subscribe = "machine/subscribe";
				public const string Unsubscribe = "machine/unsubscribe";
				public const string MarkAsIdle = "machine/markAsIdle";
				public const string Pause = "machine/pause";
				public const string Resume = "machine/resume";
				public const string Stop = "machine/stop";
				public const string FindMatchingSpools = "machine/findMatchingSpools";

				public static class Configurations
				{
					public const string All = "machine/configuration/all";
				}
			}

			public static class MachineFileStore
			{
				public const string TotalUsage = "machineFileStore/totalUsage";
				public const string MachineUsage = "machineFileStore/machineUsage";
			}
		}
	}
}
