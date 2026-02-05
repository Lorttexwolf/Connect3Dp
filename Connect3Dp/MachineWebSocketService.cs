using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;

namespace Connect3Dp
{
	public class MachineWebSocketService
	{
		private readonly IMachineCollectionService Machines;
		private readonly HashSet<MachineWebSocketContext> Sockets = [];

		public MachineWebSocketService(IMachineCollectionService machineCollectionService)
		{
			this.Machines = machineCollectionService;
			this.Machines.OnChanges += Machines_OnChanges;

			// Temp OnChange event (string machineID, MachineState updatedState)

			// Configurations
			//Machines.OnRemoved
			//Machines.OnAdded
			//Machines.OnUpdated
		}

		private void Machines_OnChanges(object? sender, MachineChangesEventArgs e)
		{
			// Pull rate.
			//e.UpdatedState
			//e.ID
			//var wantedSockets = Sockets.Where(s => s.ListeningToMachines.Contains(e.ID));
		}

		public async Task HandleIncomingConnection(WebSocket ws)
		{
			// TODO: Maake a Map extension method to invoke this function and automatically bind to group /ws

			//Example: public static IEndpointConventionBuilder MapMcp(this IEndpointRouteBuilder endpoints, [StringSyntax("Route")] string pattern = "")

			var context = new MachineWebSocketContext
			{
				WS = ws,
			};


		}
	}

	public class MachineWebSocketContext : IDisposable
	{
		public required WebSocket WS { get; init; }

		public bool ListenToLogs { get; set; } = false;

		public List<string> ListeningToMachines { get; } = [];

		public void Dispose()
		{
			this.WS.Dispose();
			this.ListeningToMachines.Clear();

			GC.SuppressFinalize(this);
		}
	}
}
