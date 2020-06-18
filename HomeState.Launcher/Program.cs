using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using HomeState.Launcher.API;
using NLog;

namespace HomeState.Launcher
{
    class Program
    {
        static string PluginPath;
        private static PluginManager PluginManager;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            _logger.Info("Starting HomeState Launcher");
            var runningProcs = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
            _logger.Info(runningProcs.Length);
            if (runningProcs.Length >= 2)
            {
                foreach (var p in runningProcs)
                {
                    if (p.Id != Process.GetCurrentProcess().Id)
                    {
                        p.Kill();
                    }
                }
            }

            var vers = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);

            _logger.Info("Starting HomeState Launcher v" + vers.FileVersion);

            PluginManager = new PluginManager();


            //Just end the App if everything is disposed. 
            while (!PluginManager.ReadyExit)
            {
                Thread.Sleep(20);
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int pid);
    }
}