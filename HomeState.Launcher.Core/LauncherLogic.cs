using System;
using System.Management;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using CefSharp;
using HomeState.Launcher.API;
using HomeState.Launcher.UI;
using Microsoft.Win32;
using Nancy;
using Nancy.Hosting.Self;
using Newtonsoft.Json;
using NLog;

namespace HomeState.Launcher.Core
{
    public class LauncherLogic : IPlugin
    {
        public static User User;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static string CONFIG_PATH = Environment.CurrentDirectory + "\\config.json";
        public static Config Config;

        public static bool WaitForInject = false;
        public static bool Ready = false;
        public static IPluginManager PluginManager;
        private NancyHost _host;

        public static string AltVPath;
        private static string _gamePAth;

        [STAThread]
        public static void ShowDialog()
        {
            var t = new Thread(() =>
            {
                var Dialog = new OpenFileDialog();
                Dialog.InitialDirectory = _gamePAth ?? "c:\\";
                Dialog.Filter = "altv (altv.exe)|altv.exe";
                Dialog.FilterIndex = 2;
                Dialog.RestoreDirectory = true;

                if (Dialog.ShowDialog(null) == DialogResult.OK)
                {
                    //Get the path of specified file
                    AltVPath = Dialog.FileName;

                    var registry = Registry.LocalMachine.CreateSubKey("SOFTWARE\\HomeState");
                    registry?.SetValue("AltVPath", AltVPath);
                    UIPlugin.Instance.ChromeBrowser.ExecuteScriptAsync("updateAltVDir", AltVPath);
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }


        public void OnLoad(IPluginManager pluginManager)
        {
            var registry = Registry.LocalMachine.CreateSubKey("SOFTWARE\\HomeState");
            var altVPath = registry?.GetValue("AltVPath");
            if (altVPath == null)
            {
                registry?.SetValue("AltVPath", "");
                AltVPath = "";
            }
            else
            {
                AltVPath = altVPath.ToString();
            }

            if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Rockstar Games\GTAV") != null ||
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\GTAV") != null)
            {
                _logger.Info("Found Steam GTAV.");

                var steam = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Rockstar Games\GTAV");

                _gamePAth = (string) steam.GetValue("InstallFolderSteam", "");
            }
            else if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Rockstar Games\Grand Theft Auto V") != null ||
                     Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Grand Theft Auto V") != null)
            {
                _logger.Info("Found Social Club GTAV.");

                var sc = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Rockstar Games\Grand Theft Auto V");

                _gamePAth = (string) sc.GetValue("InstallFolder", "");
            }
            else if (Registry.CurrentUser.OpenSubKey(@"SOFTWARE\RAGE-MP") != null)
            {
                _logger.Info("Found GTAV via RAGE-MP");

                var ragemp = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\RAGE-MP");
                _gamePAth = (string) ragemp.GetValue("game_v_path");
            }

            else
            {
                MessageBox.Show("Eine gültige Installation von GTA V konnte nicht gefunden werden.");
                _logger.Info("No GTAV Version found.");
                Environment.Exit(0);
                return;
            }

            if (System.IO.File.Exists(CONFIG_PATH))
            {
                Config = Config.LoadConfig(CONFIG_PATH);
            }
            else
            {
                Config = Config.GetDefaultConfig();
                Config.Save(CONFIG_PATH);
            }

            PluginManager = pluginManager;

            UIPlugin.Instance.OnUILoading += s =>
            {
                UIPlugin.Instance.ChromeBrowser.RegisterJsObject("launcher", new CefLauncherBridge());
                //UIPlugin.Instance.OverrideUrl = "http://192.168.178.43:8080";
                UIPlugin.Instance.OverrideUrl = "hs://res/ui.bundle/launcher.html";
                //WaitForInject = true;
                UpdateRes(Config.ResolutionMode);
            };

            UIPlugin.Instance.OnUILoaded += s => { };

            _host = new NancyHost(new Uri("http://localhost:22009"));
            _host.Start();
        }

        public void OnReady()
        {
            var startWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
            WaitForInject = false;
            startWatch.Start();
        }

        public static void UpdateRes(ResolutionMode res)
        {
            switch ((int) res)
            {
                case 0:
                {
                    var screenSize = Screen.PrimaryScreen.Bounds.Size;
                    if (screenSize.Height >= 1440 && screenSize.Width >= 2560)
                    {
                        UIPlugin.Instance.Window.Size = new System.Drawing.Size(1600, 900);
                    }
                    else
                    {
                        UIPlugin.Instance.Window.Size = new System.Drawing.Size(1280, 720);
                    }

                    break;
                }
                case 1:
                    UIPlugin.Instance.Window.Size = new System.Drawing.Size(1280, 720);
                    break;
                case 2:
                    UIPlugin.Instance.Window.Size = new System.Drawing.Size(1600, 900);
                    break;
            }
        }

        public void OnDestroy()
        {
            _host.Stop();
        }
    }


    public class LoginServer : NancyModule
    {
        private Logger _logger = LogManager.GetCurrentClassLogger();

        public LoginServer()
        {
            After.AddItemToEndOfPipeline(ctx => ctx.Response
                .WithHeader("Access-Control-Allow-Origin", "*")
                .WithHeader("Access-Control-Allow-Methods", "POST,GET")
                .WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type"));
            Get("/token", x =>
            {
                if (LauncherLogic.User != null)
                {
                    return JsonConvert.SerializeObject(new {StatusCode = 0, UserName = Base64Encode(LauncherLogic.User.UserName), Token = LauncherLogic.User.AuthCode});
                }

                //1 = Not Authenticated
                return JsonConvert.SerializeObject(new {StatusCode = 1});
            });
        }

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }
    }


    public class Config
    {
        public ResolutionMode ResolutionMode { get; set; }

        public void Save(string file)
        {
            SaveConfig(this, file);
        }

        public static Config GetDefaultConfig()
        {
            return new Config
            {
                ResolutionMode = ResolutionMode.AutoDetect
            };
        }

        public static Config LoadConfig(string file)
        {
            var str = System.IO.File.ReadAllText(file);
            return JsonConvert.DeserializeObject<Config>(str);
        }

        public static void SaveConfig(Config cfg, string file)
        {
            var str = JsonConvert.SerializeObject(cfg);
            System.IO.File.WriteAllText(file, str);
        }
    }

    public enum ResolutionMode
    {
        AutoDetect = 0,
        R1280x720 = 1,
        R1600x900 = 2
    }
}