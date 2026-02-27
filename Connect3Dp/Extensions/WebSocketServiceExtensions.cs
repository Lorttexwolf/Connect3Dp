using Connect3Dp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;

namespace Connect3Dp.Extensions
{
	public static class WebSocketServiceExtensions
	{
		public static IServiceCollection AddJeWebSocketService<C>(this IServiceCollection services, Func<string, WebSocket, C> connectionCtor) where C : IJeWebSocketClient
		{
			return services.AddSingleton(p => new JeWebSocketServer<C>(p.GetRequiredService<ILogger<JeWebSocketServer<C>>>(), connectionCtor));
		}
	}
}
