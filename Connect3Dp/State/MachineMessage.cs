using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Connect3Dp.State
{

    public class MachineMessage : IEquatable<MachineMessage?>, IUniquelyIdentifiable
    {
        public string ID { get; } 
        public string Title { get; }
        public string Body { get; }
        public DateTime IssuedAt { get; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MessageSource Source { get; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MachineMessageSeverity Severity { get; init; }

        public MessageAutoResole AutoResolve { get; init; }
        public Exception? ProgramException { get; set; }

        public MachineMessage(string title, string body, DateTime issuedAt, MessageSource source, MachineMessageSeverity severity)
        {
            Title = title;
            Body = body;
            IssuedAt = issuedAt;
            Source = source;
            Severity = severity;

            ID = GetHashCode().ToString();
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as MachineMessage);
        }

        public bool Equals(MachineMessage? other)
        {
            return other is not null &&
                   Severity == other.Severity &&
                   Source == other.Source &&
                   Title == other.Title &&
                   Body == other.Body;
        }

        public bool ReasonEquals(MachineMessage? other)
        {
            if (other is null) return false;

            return Severity == other.Severity && string.Equals(Title, other.Title, StringComparison.OrdinalIgnoreCase) && string.Equals(Body, other.Body, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Severity, Source, Title, Body);
        }

        public static bool operator ==(MachineMessage? left, MachineMessage? right)
        {
            return EqualityComparer<MachineMessage>.Default.Equals(left, right);
        }

        public static bool operator !=(MachineMessage? left, MachineMessage? right)
        {
            return !(left == right);
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

    public struct MessageAutoResole
    {
        public bool? WhenConnected;
        public MachineStatus? WhenStatus;
        public bool? WhenPrinting;
    }

    public enum MessageSource
    {
        Connector,
        Machine
    }

    public static class MachineMessageExtensions
    {
        public static bool TryFindError(this IEnumerable<MachineMessage> messages, [NotNullWhen(true)] out MachineMessage? errorMessage)
        {
            errorMessage = messages.FirstOrDefault(m => m.Severity == MachineMessageSeverity.Error);
            return errorMessage is not null;
        }

    }
}
