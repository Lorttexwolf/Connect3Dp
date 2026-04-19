using Connect3Dp.Relays.MediaMTX;
using Microsoft.Extensions.DependencyInjection;

namespace Connect3Dp.Extensions
{
	public static class MediaMTXRelayExtensions
	{
		public static IServiceCollection AddMediaMTXRelay(this IServiceCollection services, MediaMTXRelayOptions options)
		{
			services.AddSingleton(options);
			services.AddSingleton<IMediaMTXRelay, HttpMediaMTXRelay>();
			services.AddHostedService<MediaMTXCameraCoordinator>();
			return services;
		}
	}
}
