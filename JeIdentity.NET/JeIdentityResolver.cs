using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JeIdentity
{
	public static class JeIdentityResolver
	{
		private static readonly string ServiceIdentityGetterName = nameof(IJeIdentityProvider.GetServiceIdentity);

		public static ServiceIdentity Resolve(Assembly assembly)
		{
			var providerType = assembly
				.GetTypes()
				.FirstOrDefault(t => typeof(IJeIdentityProvider).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
				?? throw new InvalidOperationException($"No type implementing '{nameof(IJeIdentityProvider)}' found in assembly '{assembly.GetName().Name}'");

			var method = providerType.GetMethod(ServiceIdentityGetterName, BindingFlags.Static | BindingFlags.Public) 
				?? throw new InvalidOperationException($"'{providerType.FullName}' does not have a public static '{ServiceIdentityGetterName}' method");

			return method.Invoke(null, null) as ServiceIdentity 
				?? throw new InvalidOperationException($"'{providerType.FullName}.{ServiceIdentityGetterName}' returned null");
		}
	}
}
