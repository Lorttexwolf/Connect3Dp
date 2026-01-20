using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Connect3Dp.Constants
{
    /// <summary>
    /// Generic machine messages.
    /// </summary>
    internal static class MachineMessages
    {
        public static MachineMessage FailedToConnect { get; } = new MachineMessage
        {
            Title = "Unable to connect to Machine",
            Body = "An issue occurred connecting to this Machine",
            Severity = MachineMessageSeverity.Error,
            AutoResolve = new MessageAutoResole
            {
                OnConnected = true
            },
            Source = MessageSource.Connector,
            IssuedAt = DateTime.Now,
            LastSeenAt = DateTime.Now,
            
        };
    }
}