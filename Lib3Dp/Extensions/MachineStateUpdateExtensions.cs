using Lib3Dp.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib3Dp.Extensions
{
	public static class MachineStateUpdateExtensions
	{
		public static MachineStateUpdate SetNotifications(this MachineStateUpdate update, MachineMessage message)
		{
			var now = DateTimeOffset.UtcNow;
			update.UpdateNotifications(message, n => { n.SetIssuedAt(now); n.SetLastSeenAt(now); });
			return update;
		}
	}
}
