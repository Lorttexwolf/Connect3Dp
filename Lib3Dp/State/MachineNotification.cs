using Lib3Dp.Utilities;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Lib3Dp.State
{
	public record MachineNotification
	{
		public MachineMessage Message { get; }
		public string MessageSignature { get; }

		public DateTime IssuedAt { get; }
		public DateTime LastSeenAt { get; internal set; }

		public MachineNotification(MachineMessage content) : this(content, DateTime.Now) { }
		public MachineNotification(MachineMessage content, DateTime initiallySeenAt)
		{
			this.Message = content;
			this.MessageSignature = MachineMessage.ComputeSignature(content);
			this.IssuedAt = initiallySeenAt;
			this.LastSeenAt = initiallySeenAt;
		}
	}
}
