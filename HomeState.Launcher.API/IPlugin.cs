using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeState.Launcher.API
{
    public interface IPlugin
    {
        void OnLoad(IPluginManager pluginManager);

        /// <summary>
        /// Called when all Plugins are done loading (OnLoad Method)
        /// </summary>
        void OnReady();
        void OnDestroy();
    }
}
