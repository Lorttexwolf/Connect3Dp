using Lib3Dp;
using Lib3Dp.Files;
using Microsoft.Extensions.DependencyInjection;

namespace Connect3Dp.Extensions
{
	public static class MachineCollectionExtensions
	{
		public static IServiceCollection AddMachineConnectionCollection(this IServiceCollection services)
		{
			return services.AddSingleton<MachineConnectionCollection>();
		}
	}
}
