using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace HomeState.Launcher.API
{
    public interface IPluginManager
    {
        List<Plugin> GetLoadedPlugins();
        IResourceManager GetResourceManager();
        
        void Exit();
    }
}
