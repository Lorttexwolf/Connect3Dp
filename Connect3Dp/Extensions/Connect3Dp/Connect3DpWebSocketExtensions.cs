using Connect3Dp.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;

namespace Connect3Dp.Extensions.Connect3Dp
{
	public static partial class Connect3DpWebSocketExtensions
	{
		public static IServiceCollection AddWebSocketServer<C>(this IServiceCollection services, Func<string, WebSocket, C> connectionCtor) where C : IWebSocketClient
		{
			return services.AddSingleton(p => new WebSocketServer<C>(p.GetRequiredService<ILogger<WebSocketServer<C>>>(), connectionCtor));
		}

		public static IServiceCollection AddConnect3DpWebSocketServer(this IServiceCollection services)
		{
			return services.AddWebSocketServer((id, ws) => new Connect3DpWebSocketClient(id, ws));
		}

		public static IEndpointConventionBuilder MapWebSocketServer<C>(this IEndpointRouteBuilder endpointBuilder, string pattern) where C : IWebSocketClient
		{
			var ws = endpointBuilder.ServiceProvider.GetRequiredService<WebSocketServer<C>>();

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

		public static WebSocketServer<Connect3DpWebSocketClient> GetRequiredConnect3DpWebSocketServer(this IServiceProvider services)
		{
			return services.GetRequiredService<WebSocketServer<Connect3DpWebSocketClient>>();
		}
	}
}
