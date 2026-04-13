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
			update.UpdateNotifications(message.Id, n => { n.SetMessage(message); n.SetIssuedAt(now); n.SetLastSeenAt(now); });
			return update;
		}
	}
}
