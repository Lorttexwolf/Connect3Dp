using Connect3Dp.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;

namespace Connect3Dp.Extensions.JeWebSocket
{
	public static partial class JeWebSocketExtensions
	{
		public static IServiceCollection AddJeWebSocketService<C>(this IServiceCollection services, Func<string, WebSocket, C> connectionCtor) where C : IJeWebSocketClient
		{
			return services.AddSingleton(p => new JeWebSocketServer<C>(p.GetRequiredService<ILogger<JeWebSocketServer<C>>>(), connectionCtor));
		}

		public static IServiceCollection AddJeWebSocketServiceWithConnect3DpClient(this IServiceCollection services)
		{
			return services.AddJeWebSocketService((id, ws) => new JeWebSocketClientForConnect3Dp(id, ws));
		}

		public static IEndpointConventionBuilder MapJeWebSocketServer<C>(this IEndpointRouteBuilder endpointBuilder, string pattern) where C : IJeWebSocketClient
		{
			var ws = endpointBuilder.ServiceProvider.GetRequiredService<JeWebSocketServer<C>>();

			return endpointBuilder.MapGet(pattern, async (ctx) =>
			{
				if (!ctx.WebSockets.IsWebSocketRequest)
				{
					ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
					return;
				}
				await ws.AcceptWebSocketAsync(ctx);
			});
		}

		public static JeWebSocketServer<JeWebSocketClientForConnect3Dp> GetRequiredConnect3DpJeWebSocketServer(this IServiceProvider services)
		{
			return services.GetRequiredService<JeWebSocketServer<JeWebSocketClientForConnect3Dp>>();
		}
	}
}
