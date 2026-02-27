using Lib3Dp.Configuration;
using Lib3Dp.Files;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Connect3Dp.Extensions
{
	public static class JsonFileBasedMachineConfigurationStoreExtensions
	{
		public static IServiceCollection AddJsonFileBasedMachineConfigurationStore(this IServiceCollection services, string filePath)
		{
			services.AddSingleton<IMachineConfigurationStore>(p => new JsonFileBasedMachineConfigurationStore(p.GetRequiredService<ILogger<JsonFileBasedMachineConfigurationStore>>(), filePath));
			return services;
		}
	}
}
