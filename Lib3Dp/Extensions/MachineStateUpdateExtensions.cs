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
		public static MachineStateUpdate SetNotifications(this MachineStateUpdate update, MachineNotification notification)
		{
			update.SetNotifications(notification.MessageSignature, notification);
			return update;
		}
	}
}
