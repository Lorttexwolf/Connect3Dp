using Connect3Dp.Connectors;
using Connect3Dp.Connectors.BambuLab;
using Connect3Dp.Extensions;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Connect3Dp.State
{
    /// <summary>
    /// Represents a handle to a downloadable file exposed by a machine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="DoDownload(Stream)"/> method returns <c>true</c> on successful
    /// completion and <c>false</c> if the file could not be retrieved.
    /// </para>
    /// <para>
    /// <see cref="ID"/> uniquely identifies the file within the system and is
    /// suitable for use as a cache key, logging identifier, or lookup handle.
    /// </para>
    /// </remarks>
    public abstract class MachineFile : IUniquelyIdentifiable, IDisposable, IEquatable<MachineFile?>, ICloneable
    {
        private static readonly ConcurrentDictionary<string, MachineFile> IDToFile = new();

        [JsonIgnore] public MachineConnector Machine { get; init; }

        public string ID { get; init; }

        public abstract string? MimeType { get; }

        public MachineFile(string ID, MachineConnector Machine)
        {
            this.ID = ID;
            this.Machine = Machine;

            IDToFile[ID] = this;
        }

        ~MachineFile()
        {
            this.Dispose();
        }

        public Task<bool> Download(Stream outStream)
        {
            if (!Machine.State.Capabilities.HasFlag(MachineCapabilities.FetchFiles))
            {
                return Task.FromResult(false); // Don't throw.
            }
            try
            {
                return DoDownload(outStream);
            }
            catch (Exception)
            {
                return Task.FromResult(false);
            }
        }

        protected abstract Task<bool> DoDownload(Stream outStream);

        public static bool TryGetFile(string ID, [NotNullWhen(true)] out MachineFile? machineFile)
        {
            return IDToFile.TryGetValue(ID, out machineFile);
        }

        private bool _Disposed;
        public void Dispose()
        {
            if (!_Disposed)
            {
                IDToFile.TryRemove(this.ID, out _);
                _Disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as MachineFile);
        }

        public bool Equals(MachineFile? other)
        {
            return other is not null && ID.Equals(other.ID);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ID);
        }

        public abstract object Clone();

        public static bool operator ==(MachineFile? left, MachineFile? right)
        {
            return EqualityComparer<MachineFile>.Default.Equals(left, right);
        }

        public static bool operator !=(MachineFile? left, MachineFile? right)
        {
            return !(left == right);
        }
    }
}
