using Microsoft.AspNetCore.Mvc;
using Connect3Dp.Services;
using Lib3Dp.Files;
using Lib3Dp;

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

		// TODO: Upload files to machines.
		//[HttpGet("machineFileStore/download")]
		//public async Task<IActionResult> UploadMachineFileHandle(
		//	[FromServices] IMachineFileStore fileStore,
		//	[FromServices] MachineConnectionCollection machineConnections,
		//	IFormFile fileToUpload)
		//{
		//	//fileToUpload.
		//}
	}
}
