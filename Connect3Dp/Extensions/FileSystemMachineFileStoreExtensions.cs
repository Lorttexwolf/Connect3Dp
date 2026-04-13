using Lib3Dp.Files;
using Microsoft.Extensions.DependencyInjection;

namespace Connect3Dp.Extensions
{
	public static class FileSystemMachineFileStoreExtensions
	{
		public static IServiceCollection AddFileSystemMachineFileStore(this IServiceCollection services, FileSystemMachineFileStoreOptions options)
		{
			services.AddSingleton<IMachineFileStore>(_ => new FileSystemMachineFileStore(options));
			return services;
		}
	}

}
