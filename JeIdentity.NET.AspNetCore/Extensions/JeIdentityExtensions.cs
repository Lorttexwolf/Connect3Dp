using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JeIdentity.AspNetCore.Extensions
{
	public static class JeIdentityExtensions
	{
		public static IServiceCollection AddJeIdentity(this IServiceCollection services, ServiceIdentity serviceIdentity)
		{
			return services.AddSingleton(serviceIdentity);
		}

		/// <summary>
		/// Resolves the <see cref="ServiceIdentity"/> using the <see cref="JeIdentityResolver"/> of the calling assembly.
		/// </summary>
		public static IServiceCollection AddJeIdentity(this IServiceCollection services)
		{
			return services.AddJeIdentity(JeIdentityResolver.Resolve(Assembly.GetCallingAssembly()));
		}

		public static ServiceIdentity? GetJeIdentity(this IServiceProvider services)
		{
			return services.GetService<ServiceIdentity>();
		}

		public static ServiceIdentity GetRequiredJeIdentity(this IServiceProvider services)
		{
			return services.GetRequiredService<ServiceIdentity>();
		}

		public static IEndpointRouteBuilder MapJeIdentity(this IEndpointRouteBuilder builder, string endpointStr = "/jeIdentity")
		{
			var identity = JeIdentityResolver.Resolve(Assembly.GetCallingAssembly());

			builder.MapGet(endpointStr, () => Results.Ok(identity));

			return builder;
		}
	}
}
