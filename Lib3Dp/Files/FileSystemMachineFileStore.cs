using Lib3Dp.State;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Lib3Dp.Files;
public class FileSystemMachineFileStore : IMachineFileStore
{
	private const int MaxPathLength = 200;

	private static readonly Regex ValidIdRegex = new Regex(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);
	//private static readonly Regex ValidHashRegex = new Regex(@"^[a-fA-F0-9]{64}$", RegexOptions.Compiled);
	
	private readonly string BasePath;
	private readonly bool VerifyHashes;

	public FileSystemMachineFileStore(string basePath, bool verifyHashes = true)
	{
		BasePath = Path.GetFullPath(basePath);
		VerifyHashes = verifyHashes;

		if (!Directory.Exists(BasePath))
		{
			Directory.CreateDirectory(BasePath);
		}
	}

	public async Task Store(MachineFileHandle fileHandle, Stream fileStream)
	{
		ValidateFileHandle(fileHandle);

		var filePath = GetFilePath(fileHandle);
		var directory = Path.GetDirectoryName(filePath);

		if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}

		if (VerifyHashes)
		{
			using var sha256 = SHA256.Create();
			using var fileWriteStream = File.Create(filePath);
			using (var cryptoStream = new CryptoStream(fileWriteStream, sha256, CryptoStreamMode.Write))
			{
				await fileStream.CopyToAsync(cryptoStream);
			}

			var computedHash = Convert.ToHexString(sha256.Hash!).ToLowerInvariant();

			if (!computedHash.Equals(fileHandle.HashSHA256.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase))
			{
				File.Delete(filePath);
				throw new InvalidDataException($"Hash Mismatch: expected {fileHandle.HashSHA256}, got {computedHash}");
			}
		}
		else
		{
			using (var fileWriteStream = File.Create(filePath))
			{
				await fileStream.CopyToAsync(fileWriteStream);
			}
		}

		File.SetLastAccessTime(filePath, DateTime.UtcNow);
	}

	public Task<Stream> Stream(MachineFileHandle fileHandle)
	{
		ValidateFileHandle(fileHandle);
		var filePath = GetFilePath(fileHandle);
		var directory = Path.GetDirectoryName(filePath);

		if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}

		Stream stream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

		File.SetLastAccessTime(filePath, DateTime.UtcNow);

		return Task.FromResult(stream);
	}

	public async Task<Stream> Read(MachineFileHandle fileHandle)
	{
		ValidateFileHandle(fileHandle);
		var filePath = GetFilePath(fileHandle);

		if (!File.Exists(filePath))
		{
			throw new FileNotFoundException($"File not found");
		}

		if (VerifyHashes)
		{
			var computedHash = await ComputeFileHash(filePath);
			if (!computedHash.Equals(fileHandle.HashSHA256.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidDataException($"Stored file Hash Mismatch: expected {fileHandle.HashSHA256}, got {computedHash}");
			}
		}

		File.SetLastAccessTime(filePath, DateTime.UtcNow);

		Stream stream = File.OpenRead(filePath);
		return stream;
	}

	public bool Contains(MachineFileHandle fileHandle)
	{
		try
		{
			ValidateFileHandle(fileHandle);
			var filePath = GetFilePath(fileHandle);
			return File.Exists(filePath);
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	public Task Delete(MachineFileHandle fileHandle)
	{
		return Task.Run(() =>
		{
			ValidateFileHandle(fileHandle);
			var filePath = GetFilePath(fileHandle);

			if (File.Exists(filePath))
			{
				File.Delete(filePath);

				// Clean up empty directories
				var directory = Path.GetDirectoryName(filePath);
				if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
				{
					if (!Directory.EnumerateFileSystemEntries(directory).Any())
					{
						Directory.Delete(directory);
					}
				}
			}
		});
	}

	public Task<StorageInfo> GetStorageInfo()
	{
		return Task.Run(() => ComputeStorageInfo(BasePath));
	}

	public Task<StorageInfo> GetStorageInfo(string machineId)
	{
		if (string.IsNullOrWhiteSpace(machineId) || !ValidIdRegex.IsMatch(machineId))
		{
			throw new ArgumentException("Invalid MachineID");
		}

		var machinePath = Path.Combine(BasePath, machineId);
		return Task.Run(() => ComputeStorageInfo(machinePath));
	}

	public Task<PruneResult> Prune(PruneOptions options)
	{
		return Task.Run(() =>
		{
			var result = new PruneResult();

			var searchPath = string.IsNullOrWhiteSpace(options.MachineID)
				? BasePath
				: Path.Combine(BasePath, options.MachineID);

			if (!Directory.Exists(searchPath))
			{
				return result;
			}

			var files = Directory.GetFiles(searchPath, "*", SearchOption.AllDirectories)
				.Select(f => new FileInfo(f))
				.ToList();

			// Filter by age
			if (options.OlderThan.HasValue)
			{
				var cutoffDate = (DateTime.UtcNow - options.OlderThan.Value);
				files = files.Where(f => f.CreationTimeUtc < cutoffDate).ToList();
			}

			// Filter by last access
			if (options.NotAccessedSince.HasValue)
			{
				files = files.Where(f => f.LastAccessTimeUtc < options.NotAccessedSince.Value).ToList();
			}

			// Delete files
			foreach (var file in files)
			{
				if (!options.DryRun)
				{
					try
					{
						file.Delete();
						result.DeletedFiles.Add(file.FullName);
						result.FilesDeleted++;
						result.BytesFreed += file.Length;
					}
					catch (Exception)
					{
						// Log error but continue
					}
				}
				else
				{
					result.DeletedFiles.Add(file.FullName);
					result.FilesDeleted++;
					result.BytesFreed += file.Length;
				}
			}

			// Clean up empty directories
			if (!options.DryRun)
			{
				CleanupEmptyDirectories(searchPath);
			}

			return result;
		});
	}

	private static StorageInfo ComputeStorageInfo(string path)
	{
		if (!Directory.Exists(path))
		{
			return new StorageInfo
			{
				TotalBytes = 0,
				FileCount = 0
			};
		}

		var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
			.Select(f => new FileInfo(f))
			.ToList();

		return new StorageInfo
		{
			TotalBytes = files.Sum(f => f.Length),
			FileCount = files.Count,
			OldestFileDate = files.Any() ? files.Min(f => f.CreationTimeUtc) : (DateTime?)null,
			NewestFileDate = files.Any() ? files.Max(f => f.CreationTimeUtc) : (DateTime?)null
		};
	}

	private static void CleanupEmptyDirectories(string path)
	{
		foreach (var directory in Directory.GetDirectories(path))
		{
			CleanupEmptyDirectories(directory);

			if (!Directory.EnumerateFileSystemEntries(directory).Any())
			{
				try
				{
					Directory.Delete(directory);
				}
				catch
				{
					// Ignore errors
				}
			}
		}
	}

	private static void ValidateFileHandle(MachineFileHandle fileHandle)
	{
		if (string.IsNullOrWhiteSpace(fileHandle.MachineID))
			throw new ArgumentException("MachineID cannot be empty");

		if (fileHandle.MachineID.Length > 100)
			throw new ArgumentException("MachineID too long");

		if (!ValidIdRegex.IsMatch(fileHandle.MachineID))
			throw new ArgumentException("MachineID contains invalid characters");
	}

	private string GetFilePath(MachineFileHandle fileHandle)
	{
		var safeFileName = fileHandle.HashSHA256.ToLowerInvariant();
		var subDir = safeFileName[..2];

		var fullPath = Path.GetFullPath(Path.Combine(BasePath, fileHandle.MachineID, subDir, safeFileName));

		if (!fullPath.StartsWith(BasePath + Path.DirectorySeparatorChar) && !fullPath.Equals(BasePath))
		{
			throw new UnauthorizedAccessException("Path traversal attempt detected");
		}

		if (fullPath.Length > MaxPathLength) throw new ArgumentException("Resulting path too long");

		return fullPath;
	}

	private static async Task<string> ComputeFileHash(string filePath)
	{
		using var sha256 = SHA256.Create();
		using var fileStream = File.OpenRead(filePath);

		var hash = await sha256.ComputeHashAsync(fileStream);
		return Convert.ToHexString(hash).ToLowerInvariant();
	}
}
