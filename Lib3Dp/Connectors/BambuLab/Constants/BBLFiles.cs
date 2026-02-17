using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lib3Dp.State;

namespace Lib3Dp.Connectors.BambuLab.Constants
{
	internal class BBLFiles
	{
		private const string Prefix3MF = "3MF/";
		private const string ThumbnailSuffix = "/Thumbnail";
		private const string MimeType3MF = "model/3mf";
		private const string MimeTypePNG = "image/png";

		/// <summary>
		/// Utilized as the formatter for <see cref="MachineFileHandle"/> to represent a 3MF file.
		/// </summary>
		public static MachineFileHandle HandleAs3MF(string machineID, string path, string hash)
		{
			return new MachineFileHandle(machineID, $"{Prefix3MF}{path}", MimeType3MF, hash);
		}

		/// <summary>
		/// Utilized as the formatter for <see cref="MachineFileHandle"/> to represent a 3MF thumbnail (PNG).
		/// </summary>
		public static MachineFileHandle HandleAs3MFThumbnail(string machineID, string path, string hash)
		{
			return new MachineFileHandle(machineID, $"{Prefix3MF}{path}{ThumbnailSuffix}", MimeTypePNG, hash);
		}

		/// <summary>
		/// Attempts to parse a <see cref="MachineFileHandle"/> as a 3MF file handle and extract the local path.
		/// </summary>
		public static bool TryParseAs3MFHandle(MachineFileHandle inputHandle, [NotNullWhen(true)] out string? localPath)
		{
			localPath = null;

			// Check if it's a 3MF file (not a thumbnail)
			if (!inputHandle.URI.StartsWith(Prefix3MF, StringComparison.Ordinal))
				return false;

			if (inputHandle.URI.EndsWith(ThumbnailSuffix, StringComparison.Ordinal))
				return false;

			// Check MIME type
			if (!string.Equals(inputHandle.MIME, MimeType3MF, StringComparison.OrdinalIgnoreCase))
				return false;

			// Extract the local path by removing the prefix
			localPath = inputHandle.URI.Substring(Prefix3MF.Length);
			return true;
		}

		/// <summary>
		/// Attempts to parse a <see cref="MachineFileHandle"/> as a 3MF thumbnail handle and extract the local path.
		/// </summary>
		public static bool TryParseAs3MFThumbnailHandle(MachineFileHandle inputHandle, [NotNullWhen(true)] out string? localPath)
		{
			localPath = null;

			// Check if it's a thumbnail
			if (!inputHandle.URI.StartsWith(Prefix3MF, StringComparison.Ordinal))
				return false;

			if (!inputHandle.URI.EndsWith(ThumbnailSuffix, StringComparison.Ordinal))
				return false;

			// Check MIME type
			if (!string.Equals(inputHandle.MIME, MimeTypePNG, StringComparison.OrdinalIgnoreCase))
				return false;

			// Extract the local path by removing both prefix and suffix
			int startIndex = Prefix3MF.Length;
			int length = inputHandle.URI.Length - Prefix3MF.Length - ThumbnailSuffix.Length;

			if (length <= 0)
				return false;

			localPath = inputHandle.URI.Substring(startIndex, length);
			return true;
		}

		/// <summary>
		/// Checks if a <see cref="MachineFileHandle"/> represents a 3MF file or thumbnail.
		/// </summary>
		public static bool Is3MFRelated(MachineFileHandle inputHandle)
		{
			return inputHandle.URI.StartsWith(Prefix3MF, StringComparison.Ordinal);
		}
	}
}
