using Lib3Dp.Connectors;
using System.Diagnostics.CodeAnalysis;

namespace Lib3Dp.Plugins
{
	public interface IPlugin<T, D> where T : IPlugin<T, D>
	{
		public abstract bool RegisterConnector(MachineConnection connector);

		public abstract bool IsConnectorRegistered(MachineConnection connector);

		public abstract D? GetConnectorPluginData(MachineConnection connector);

		internal static abstract bool TryGetInstance([NotNullWhen(true)] out T? instance);
	}
}
