using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using AdysTech.CredentialManager;
using CefSharp;
using HomeState.Launcher.UI;
using Newtonsoft.Json;
using NLog;
using RestSharp;

namespace HomeState.Launcher.Core
{
    public class CefLauncherBridge
    {
        private Logger _logger = LogManager.GetCurrentClassLogger();

        public string News;

        public System.Timers.Timer UpdateNewsTimer;
        private bool _isReady = false;

        public CefLauncherBridge()
        {
            News = new WebClient().DownloadString("http://launcher.homestate.de/news.json");
        }

        public void Ready()
        {
            _isReady = true;
        }

        public string GetNews()
        {
            return News;
        }

        public string GetVersion()
        {
            var c = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
            return $"{c.ProductName} {c.ProductVersion}";
        }

        public object GetLoadedPlugins()
        {
            return JsonConvert.SerializeObject(UIPlugin.PluginManager.GetLoadedPlugins());
        }

        public string GetAltVPath => LauncherLogic.AltVPath;

        public void SetAltVPath()
        {
            LauncherLogic.ShowDialog();
        }

        public void StartGame()
        {
            if (LauncherLogic.Ready && LauncherLogic.User != null)
            {
                //UIPlugin.Instance.Window.WindowState = FormWindowState.Minimized;
                //RageLauncher.Instance.StartGame("game.homestate.eu", 22005,  LauncherLogic.GTAV.Command, LauncherLogic.GTAV.Type);


                if (LauncherLogic.AltVPath == "")
                {
                    LauncherLogic.ShowDialog();
                }
                else
                {
                    if (!System.IO.File.Exists(LauncherLogic.AltVPath))
                    {
                        LauncherLogic.ShowDialog();
                        return;
                    }

                    _logger.Info("Stating alt:V " + LauncherLogic.AltVPath);
                    UIPlugin.Instance.Window.WindowState = FormWindowState.Minimized;
                    var p = new Process();
                    p.StartInfo.WorkingDirectory = LauncherLogic.AltVPath.Replace("altv.exe", "");
                    p.StartInfo.FileName = "altv.exe";
                    p.StartInfo.Arguments = "-connecturl altv.homestate.de:80";
                    p.StartInfo.UseShellExecute = true;
                    p.StartInfo.RedirectStandardOutput = false;
                    p.Start();
                }
            }

            LauncherLogic.WaitForInject = false;
        }

        public User GetUser()
        {
            return LauncherLogic.User;
        }

        public bool SavedCredentials()
        {
            return CredentialManager.GetCredentials("HomeState.de/Launcher") != null;
        }

        public string GetSavedUserName()
        {
            return CredentialManager.GetCredentials("HomeState.de/Launcher")?.UserName;
        }

        public void AutoLogin(string twoFaCode)
        {
            if (SavedCredentials())
            {
                var creds = CredentialManager.GetCredentials("HomeState.de/Launcher");
                Login(creds.UserName, creds.Password, twoFaCode, false);
            }
            else
            {
                UIPlugin.Instance.ChromeBrowser.ExecuteScriptAsync("loginFailed", "Fehler!");
            }
        }

        public void Login(string username, string password, string twoFaCode, bool saveCreds)
        {
            //_logger.Info($"{username} {password} {twoFaCode}");
            //new task so we dont block the ui.
            new Task(() =>
            {
                var homestateEndpoint = new RestClient("https://www.homestate.eu/apiv2/");

                var loginRequest = new RestRequest("launcherLogin.php", Method.POST, RestSharp.DataFormat.Json);
                loginRequest.AddParameter("username", username);
                loginRequest.AddParameter("password", password);
                loginRequest.AddParameter("2fa", twoFaCode);

                var loginResponse = homestateEndpoint.Execute<LoginResponse>(loginRequest);

                try
                {
                    if (loginResponse.Data.StatusCode == 0)
                    {
                        _logger.Info("Login ok.");
                        if (saveCreds) CredentialManager.SaveCredentials("HomeState.de/Launcher", new NetworkCredential(username, password));
                        LauncherLogic.User = loginResponse.Data.UserData;
                        UIPlugin.Instance.ChromeBrowser.ExecuteScriptAsync("showMainWindow();");

                        UIPlugin.Instance.ChromeBrowser.ExecuteScriptAsync("showPlayButton();");
                        LauncherLogic.Ready = true;
                    }
                    else
                    {
                        switch (loginResponse.Data.StatusCode)
                        {
                            case 4:
                            {
                                UIPlugin.Instance.ChromeBrowser.ExecuteScriptAsync("loginFailed",
                                    "Falsche Zugangsdaten!");
                                break;
                            }
                            case 6:
                            {
                                UIPlugin.Instance.ChromeBrowser.ExecuteScriptAsync("loginFailed",
                                    "Keine Berechtigung!");
                                break;
                            }
                            default:
                            {
                                UIPlugin.Instance.ChromeBrowser.ExecuteScriptAsync("loginFailed", "Fehler!");
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    UIPlugin.Instance.ChromeBrowser.ExecuteScriptAsync("loginFailed", "Fehler!");
                    _logger.Error(e);
                }
            }).Start();
        }

        public void Exit()
        {
            Application.Exit();
        }

        public void SetRes(int typ)
        {
            var res = (ResolutionMode) typ;

            LauncherLogic.Config.ResolutionMode = res;
            LauncherLogic.Config.Save(LauncherLogic.CONFIG_PATH);
            LauncherLogic.UpdateRes(LauncherLogic.Config.ResolutionMode);
        }

        public int GetRes()
        {
            return (int) LauncherLogic.Config.ResolutionMode;
        }

        public bool IsDebug()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }

        public void ResetPw()
        {
            Process.Start("https://www.homestate.eu/index.php?lost-password/");
        }

        public void Minimize()
        {
            UIPlugin.Instance.Window.WindowState = FormWindowState.Minimized;
        }
    }
}