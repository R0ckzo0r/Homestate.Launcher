using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeState.Launcher.API
{
    public class Plugin
    {
        
        public string Name { get; set; }
        public string AssemblyFullName { get; set; }
        public string Path { get; set; }
        public PluginInfo PluginInfo { get; set; }

        [JsonIgnore]
        public IPlugin PluginInstance { get; set; }
    }
}
