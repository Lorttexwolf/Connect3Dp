using Lib3Dp.Utilities;
using PartialBuilderSourceGen.Attributes;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Lib3Dp.State
{
	public class MachineNotification(MachineMessage content, DateTimeOffset initiallySeenAt) : IEquatable<MachineNotification?>
	{
		public MachineMessage Message { get; } = content;
		public string MessageSignature { get; } = MachineMessage.ComputeSignature(content);

		public DateTimeOffset IssuedAt { get; } = initiallySeenAt;
		public DateTimeOffset LastSeenAt { get; internal set; } = initiallySeenAt;

		public MachineNotification(MachineMessage content) : this(content, DateTimeOffset.Now) { }

		public override bool Equals(object? obj)
		{
			return Equals(obj as MachineNotification);
		}

		public bool Equals(MachineNotification? other)
		{
			return other is not null
				&& Message.Equals(other.Message)
				&& MessageSignature == other.MessageSignature;
		}

		public override int GetHashCode()
		{
			return Message.GetHashCode();
		}
	}
}
