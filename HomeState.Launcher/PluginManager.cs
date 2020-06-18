using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using HomeState.Launcher.API;
using NLog;

namespace HomeState.Launcher
{
    class PluginManager : IPluginManager
    {
        public List<Plugin> Plugins;
        public List<PluginInfo> LoadQueue;

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        private IResourceManager _resourceManager;

        public bool IsShuttingDown = false;
        public bool ReadyExit = false;

        public List<PluginInfo> PluginLoadQueue;

        public List<Plugin> GetLoadedPlugins()
        {
            return Plugins.ToList();
        }

        public IResourceManager GetResourceManager()
        {
            return _resourceManager;
        }

        public void Exit()
        {
            if (IsShuttingDown)
            {
                _logger.Warn("Already shutting down...");
                return;
            }

            IsShuttingDown = true;

            foreach (var item in Plugins)
            {
                _logger.Info($"Disabling Plugin {item.PluginInfo.DisplayName} [{item.PluginInfo.MainAssembly}]");
                item.PluginInstance.OnDestroy();
            }

            Plugins.Clear();
            ReadyExit = true;
        }

        public void DisablePlugins()
        {
            foreach (var item in Plugins)
            {
                _logger.Info($"Disabling Plugin {item.PluginInfo.DisplayName} [{item.PluginInfo.MainAssembly}]");
                item.PluginInstance.OnDestroy();
                
            }

            Plugins.Clear();
        }

        public PluginManager()
        {
            var PluginPath = Environment.CurrentDirectory + "\\plugins";
            Plugins = new List<Plugin>();


            PluginLoadQueue = new List<PluginInfo>();

            
          

            _logger.Info("Initializing Resource Manager");


            _resourceManager = new ResourceManager();



            try
            {
                _logger.Info($"Loading Plugins from {PluginPath}");

                if (!Directory.Exists(PluginPath))
                {
                    Directory.CreateDirectory(PluginPath);
                }

                foreach (var item in Directory.GetDirectories(PluginPath))
                {
                    //_logger.Info(item);

                    if (File.Exists(item + "\\plugin.json"))
                    {
                        var str = JsonConvert.DeserializeObject<PluginInfo>(File.ReadAllText(item + "\\plugin.json"));

                        if (str == null)
                        {
                            _logger.Info($"Could not load Plugin {new DirectoryInfo(item).Name}");
                        }
                        else
                        {

                            str.InternalPath = item;
                            if (str.Dependencies != null && str.Dependencies.Count >= 1)
                            {

                                var ready = true;
                                foreach (var dependency in str.Dependencies)
                                {

                                    if (!Plugins.Exists(x =>
                                        x.Name == dependency.Name && x.PluginInfo.Version == dependency.Version))
                                        ready = false;

                                    //_logger.Info($"Require {dependency.Name} {dependency.Version}");

                                }

                                if (!ready)
                                {
                                    _logger.Debug("Waiting for Deps on Plugin " + str.Name);
                                    PluginLoadQueue.Add(str);
                                    continue;

                                }
                            }

                            if (!File.Exists(item + "\\" + str.MainAssembly))
                            {

                                _logger.Info($"Could not load Plugin {new DirectoryInfo(item).Name}");
                                continue;
                            }

                            LoadPlugin(item, str);
                            foreach (var queuePlugin in PluginLoadQueue)
                            {
                                var ready = true;
                                foreach (var dep in queuePlugin.Dependencies)
                                {
                                    //_logger.Info($"Checking Dep {queuePlugin.Name}: {dep.Name}");
                                    if (!Plugins.Exists(x =>
                                        x.Name == dep.Name))
                                        ready = false;
                                }

                                if (ready)
                                {
                                    _logger.Debug("Loaded all Deps for " + queuePlugin.Name);
                                    LoadPlugin(queuePlugin.InternalPath, queuePlugin);
                                }

                            }

                            foreach (var loadedPlugin in Plugins)
                            {
                                if (PluginLoadQueue.Exists(x =>
                                    x.Name == loadedPlugin.Name && x.Version == loadedPlugin.PluginInfo.Version))
                                {
                                    PluginLoadQueue.Remove(loadedPlugin.PluginInfo);
                                }
                            }
                        }
                    }

                    foreach (var queuePlugin in PluginLoadQueue)
                    {
                        var ready = true;
                        foreach (var dep in queuePlugin.Dependencies)
                        {
                            //_logger.Info($"Checking Dep {queuePlugin.Name}: {dep.Name}");
                            if (!Plugins.Exists(x =>
                                x.Name == dep.Name))
                                ready = false;
                        }

                        if (ready)
                        {
                            _logger.Debug("Loaded all Deps for " + queuePlugin.Name);
                            LoadPlugin(queuePlugin.InternalPath, queuePlugin);
                        }
                    }

                    foreach (var loadedPlugin in Plugins)
                    {
                        if (PluginLoadQueue.Exists(x =>
                            x.Name == loadedPlugin.Name && x.Version == loadedPlugin.PluginInfo.Version))
                        {
                            PluginLoadQueue.Remove(loadedPlugin.PluginInfo);
                        }
                    }
                }

            }
            catch (System.Exception e)
            {
                _logger.Fatal(e);
            }
            
            foreach (var item in PluginLoadQueue)
            {
                _logger.Warn($"Could not load Plugin {item.Name}. Reason: Missing Dependencies");
            }

            try
            {
                foreach (var item in Plugins)
                {
                    item.PluginInstance.OnReady();
                }
            }
            catch (System.Exception e)
            {
                _logger.Fatal(e);
            }
        }

        void LoadPlugin(string path, PluginInfo info)
        {

            _logger.Info($"Loading Plugin {info.Name} [{info.MainAssembly}]");
            Assembly c;

            if (IsNetworkPath(Environment.CurrentDirectory))
            {
                c = Assembly.UnsafeLoadFrom(path + "\\" + info.MainAssembly);
            }
            else
            {
                c = Assembly.LoadFrom(path + "\\" + info.MainAssembly);
            }

            if (Plugins.FirstOrDefault(x => x.Name == info.Name || x.AssemblyFullName == c.FullName) != null)
            {
                _logger.Warn("Plugin already loaded.");
                return;
            }

            var typ = typeof(IPlugin);
            var type = c.GetTypes().FirstOrDefault(p => typ.IsAssignableFrom(p));

            if (type != null)
            {
                if (info.Assets != null && info.Assets.Count > 0)
                {
                    foreach (var item in info.Assets)
                    {
                        _resourceManager.RegisterBundle(path + "\\" + item);
                    }
                }

                var pluginInstance = (IPlugin) Activator.CreateInstance(type);
                Plugins.Add(new Plugin
                {
                    Name = info.Name,
                    Path = path,
                    AssemblyFullName = c.FullName,
                    PluginInstance = pluginInstance,
                    PluginInfo = info
                });

                pluginInstance.OnLoad(this);
            }
            else
            {
                _logger.Warn($"Could not load Main Assembly from {info.Name}");
            }
        }
        
        public static bool IsNetworkPath(string path)
        {
            if (!path.StartsWith(@"/") && !path.StartsWith(@"\"))
            {
                var rootPath = Path.GetPathRoot(path); 
                DriveInfo driveInfo = new System.IO.DriveInfo(rootPath); 
                return driveInfo.DriveType == DriveType.Network;
            }

            return true; // is a UNC path
        }
        
    }

    public static class ProcessExtensions
    {
        private static string FindIndexedProcessName(int pid)
        {
            var processName = Process.GetProcessById(pid).ProcessName;
            var processesByName = Process.GetProcessesByName(processName);
            string processIndexName = null;

            for (var index = 0; index < processesByName.Length; index++)
            {
                processIndexName = index == 0 ? processName : processName + "#" + index;
                var processId = new PerformanceCounter("Process", "ID Process", processIndexName);
                if ((int) processId.NextValue() == pid)
                {
                    return processIndexName;
                }
            }

            return processIndexName;
        }

        private static Process FindPidFromIndexedProcessName(string indexedProcessName)
        {
            var parentId = new PerformanceCounter("Process", "Creating Process ID", indexedProcessName);
            return Process.GetProcessById((int) parentId.NextValue());
        }

        public static Process Parent(this Process process)
        {
            return FindPidFromIndexedProcessName(FindIndexedProcessName(process.Id));
        }
    }
}
