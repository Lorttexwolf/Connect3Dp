using Lib3Dp.State;

namespace Lib3Dp.Files
{
	// Examples, FileSystemMachineFileStore, S3MachineFileStore
	public interface IMachineFileStore
	{
		/// <summary>
		/// Opens a writeable <see cref="System.IO.Stream"/> to interact with the <paramref name="fileHandle"/>.
		/// </summary>
		Task<Stream> Stream(MachineFileHandle fileHandle);
		Task Store(MachineFileHandle fileHandle, Stream fileStream);
		Task<Stream> Read(MachineFileHandle fileHandle);

		bool Contains(MachineFileHandle fileHandle);

		Task<StorageInfo> GetStorageInfo();
		Task<StorageInfo> GetStorageInfo(string machineID);

		Task Delete(MachineFileHandle fileHandle);
		Task<PruneResult> Prune(PruneOptions options);
	}

	public record struct StorageInfo(long TotalBytes, long FileCount, DateTime? OldestFileDate, DateTime? NewestFileDate)
	{
		public readonly double TotalMB => TotalBytes / (1024.0 * 1024.0);
		public readonly double TotalGB => TotalBytes / (1024.0 * 1024.0 * 1024.0);
	}

	public record struct PruneOptions(bool DryRun, TimeSpan? OlderThan, DateTime? NotAccessedSince, string? MachineID);

	public record struct PruneResult(int FilesDeleted, long BytesFreed, List<string> DeletedFiles)
	{
		public readonly double FreedMB => BytesFreed / (1024.0 * 1024.0);
		public readonly double FreedGB => BytesFreed / (1024.0 * 1024.0 * 1024.0);
	}
}
