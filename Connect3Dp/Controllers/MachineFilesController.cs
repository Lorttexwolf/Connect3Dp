using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Connect3Dp.Services;
using Lib3Dp.Files;
using Lib3Dp;
using System.Security.Cryptography;

namespace Connect3Dp.Controllers
{
	[ApiController]
	public class MachineFilesController : ControllerBase
	{
		[HttpGet("machineFileStore/download")]
		public async Task<IActionResult> DownloadMachineFileHandle(
			[FromServices] IMachineFileStore fileStore,
			[FromServices] MachineConnectionCollection machineConnections,
			[FromBody] MachineFileHandle machineFileHandle)
		{
			Stream downloadStream;

			try
			{
				if (fileStore.Contains(machineFileHandle))
				{
					// Download from the Machine File Store

					downloadStream = await fileStore.Read(machineFileHandle);
				}
				else if (machineConnections.Connections.TryGetValue(machineFileHandle.MachineID, out var machineConnection))
				{
					downloadStream = await machineConnection.DownloadFile(machineFileHandle);
				}
				else
				{
					return NotFound();
				}
			}
			catch (Exception)
			{
				return NotFound();
			}

			return File(downloadStream, machineFileHandle.MIME);
		}

		[HttpPost("machineFileStore/upload")]
		public async Task<IActionResult> UploadMachineFile(
			[FromServices] IMachineFileStore fileStore,
			[FromQuery] string machineId,
			[FromQuery] string uri,
			[FromQuery] string? mimeType,
			IFormFile file)
		{
			if (string.IsNullOrWhiteSpace(machineId))
				return BadRequest("machineId is required.");

			if (string.IsNullOrWhiteSpace(uri))
				return BadRequest("uri is required.");

			var resolvedMime = mimeType ?? file.ContentType ?? "application/octet-stream";

			// Buffer the upload so we can hash and store from the same bytes.
			using var buffer = new MemoryStream();
			await using (var uploadStream = file.OpenReadStream())
				await uploadStream.CopyToAsync(buffer);

			string hash;
			using (var sha256 = SHA256.Create())
				hash = Convert.ToHexString(sha256.ComputeHash(buffer.ToArray())).ToLowerInvariant();

			var handle = new MachineFileHandle(machineId, uri, resolvedMime, hash);

			buffer.Position = 0;
			await fileStore.Store(handle, buffer);

			return Ok(handle);
		}
	}
}
