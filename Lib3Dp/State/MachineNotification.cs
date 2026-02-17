using Lib3Dp.Utilities;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Lib3Dp.State
{
	public readonly record struct MachineMessage(
		string Title, 
		string Body,
		[property: JsonConverter(typeof(JsonStringEnumConverter))] MachineMessageSeverity Severity,
		[property: JsonConverter(typeof(JsonStringEnumConverter))] MachineMessageActions ManualResolve,
		MachineMessageAutoResole AutoResolve)
	{
		public static string ComputeSignature(in MachineMessage content)
		{
			int byteCount = Encoding.UTF8.GetByteCount(content.Title)
				+ Encoding.UTF8.GetByteCount(content.Body)
				+ sizeof(MachineMessageActions)
				+ sizeof(MachineMessageSeverity);

			Span<byte> data = stackalloc byte[byteCount];

			int offset = 0;

			offset += Encoding.UTF8.GetBytes(content.Title, data[offset..]);
			offset += Encoding.UTF8.GetBytes(content.Body, data[offset..]);

			// Utilize BinaryPrimitives; meant for Span.

			BinaryPrimitives.WriteInt32LittleEndian(data[offset..], (int)content.Severity);
			offset += sizeof(int);
			BinaryPrimitives.WriteInt32LittleEndian(data[offset..], (int)content.ManualResolve);
			offset += sizeof(int);

			Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
			SHA256.HashData(data[..offset], hash);

			return Convert.ToHexString(hash);
		}

		public override string ToString()
		{
			return $"{Severity}: {Title} {Body}";
		}
	}
		
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

	public enum MachineMessageSeverity
	{
		Info = 0,
		Success = 1,
		Warning = 2,
		Error = 3
	}

	/// <summary>
	/// Actions which can be taken to resolve this message.
	/// </summary>
	[Flags]
	public enum MachineMessageActions
	{
		None = 0,
		Resume = 1,
		Pause = 2,
		Cancel = 4,
		Refresh = 8,
		CheckConnection = 16,
		UnsupportedFirmware = 32,
		ClearBed = 64
	}

	public record struct MachineMessageAutoResole(bool? WhenConnected, MachineStatus? WhenStatus, bool? WhenPrinting);

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
