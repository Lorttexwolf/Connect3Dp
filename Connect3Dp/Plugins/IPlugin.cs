using Connect3Dp.Connectors;
using Connect3Dp.Plugins.OME;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Connect3Dp.Plugins
{
    public interface IPlugin<T, D> where T : IPlugin<T, D>
    {
        public abstract bool RegisterConnector(MachineConnector connector);

        public abstract bool IsConnectorRegistered(MachineConnector connector);

        public abstract D? GetConnectorPluginData(MachineConnector connector);

        internal static abstract bool TryGetInstance([NotNullWhen(true)] out T? instance);
    }
}
