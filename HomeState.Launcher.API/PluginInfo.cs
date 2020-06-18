using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeState.Launcher.API
{
    public class PluginInfo
    {
        public string Name { get; set; }
        public string InternalPath { get; set; }
        public string DisplayName { get; set; }
        public string Version { get; set; }
        public string MainAssembly { get; set; }
        public List<PluginDependency> Dependencies { get; set; }
        public List<string> Assets { get; set; }
    }

    public class PluginDependency
    {
        public string Name { get; set; }
        public string Version { get; set; }
    }
}
