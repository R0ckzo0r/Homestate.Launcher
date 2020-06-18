using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using Microsoft.Win32;
using File = System.IO.File;

namespace HomeState.Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private const string UPDATE_URL = "http://launcher.homestate.de/update.xml";

        private readonly WebClient _webClient;
        private readonly string _installDir;
        private readonly UpdateManifest _updateManifest;
        private readonly bool _forceUpdate;

        public MainWindow()
        {
            _webClient = new WebClient();
            InitializeComponent();

            try
            {
                if (!IsElevated)
                {
                    var p = new Process
                    {
                        StartInfo =
                        {
                            FileName = Assembly.GetEntryAssembly().Location,
                            Verb = "runas",
                            Arguments = string.Join(" ", Environment.GetCommandLineArgs().Skip(1))
                        }
                    };
                    p.Start();
                    Environment.Exit(-1);
                }

                var useCurrentPath =
                    Array.Exists(Environment.GetCommandLineArgs(), x => x.Contains("--useCurrentPath"));
                _forceUpdate = Array.Exists(Environment.GetCommandLineArgs(), x => x.Contains("--forceUpdate"));

                if (useCurrentPath)
                {
                    Console.WriteLine("Skipping Registry Checks & Installation.");
                }

                Console.WriteLine("Downloading Manifest...");
                XmlSerializer serializer = new XmlSerializer(typeof(UpdateManifest));
                _updateManifest = (UpdateManifest)serializer.Deserialize(new StringReader(new WebClient().DownloadString(UPDATE_URL)));

                var installationPath = Environment.GetEnvironmentVariable("ProgramFiles") + "\\HomeState\\";
                var subKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\HomeState", true);

                Console.WriteLine(_updateManifest.UpdaterVersion);

                _installDir = installationPath;
                var _updateUpdater = Array.Exists(Environment.GetCommandLineArgs(), x => x.Contains("--update"));

                if ((_updateManifest.UpdaterVersion != FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).FileVersion) && subKey != null && !_updateUpdater)
                {
                   
                    //Needs to update.
                    if (File.Exists(Path.GetTempPath() + "\\Launcher.exe"))
                    {
                        File.Delete(Path.GetTempPath() + "\\Launcher.exe");
                    }

                    File.Copy(Assembly.GetEntryAssembly().Location, Path.GetTempPath() + "\\Launcher.exe");
                    var p = new Process
                    {
                        StartInfo =
                        {
                            FileName = Path.GetTempPath() + "\\Launcher.exe",
                            Verb = "runas",
                            Arguments = "--update"
                        }
                    };
                    p.Start();
                    Environment.Exit(-1);
                }

                if (_updateUpdater)
                {
                    if(subKey == null)
                    {
                        Environment.Exit(0);
                    }
                    if (File.Exists(installationPath + "Launcher.exe"))
                    {
                        File.Delete(installationPath + "Launcher.exe");
                    }
                    new WebClient().DownloadFile(_updateManifest.UpdaterFile, installationPath + "Launcher.exe");
                    var p = new Process
                    {
                        StartInfo =
                        {
                            FileName = installationPath + "Launcher.exe",
                            Verb = "runas"
                        }
                    };
                    p.Start();

                    Environment.Exit(0);
                }

                if (!useCurrentPath)
                {
                    if (subKey != null)
                    {
                        var installPathRegistry = (string) subKey.GetValue("InstallationPath", installationPath);
                        _installDir = installPathRegistry;
                        Console.WriteLine(Assembly.GetEntryAssembly().Location);
                        Console.WriteLine(_installDir + "Launcher.exe");
                        Console.WriteLine(Assembly.GetEntryAssembly().Location == _installDir);

                        if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + @"\Programme\HomeState\"))
                        {
                            Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + @"\Programme\HomeState\");
                            appShortcutToStartmenu("HomeState Launcher");
                        }  

                        
                        if (!string.Equals(Assembly.GetEntryAssembly().Location, _installDir + "Launcher.exe"))
                        {
                            if (!File.Exists(_installDir + "Launcher.exe"))
                            {
                                File.Copy(Process.GetCurrentProcess().MainModule.FileName,
                                    _installDir + "Launcher.exe");
                            }

                            Process.Start(_installDir + "Launcher.exe");
                            Environment.Exit(0);
                        }
                    }
                    else
                    {
                        Console.WriteLine(@"RegKey not found. HomeState Launcher is not installed");
                        Console.WriteLine(_installDir);

                        if (Directory.Exists(_installDir))
                        {
                            Directory.Delete(_installDir, true);
                        }

                        Directory.CreateDirectory(_installDir);

                        Console.WriteLine(Process.GetCurrentProcess().MainModule.FileName);

                        File.Copy(Process.GetCurrentProcess().MainModule.FileName, _installDir + "Launcher.exe");

                        subKey = Registry.LocalMachine.CreateSubKey("SOFTWARE\\HomeState");
                        subKey?.SetValue("InstallationPath", _installDir, RegistryValueKind.String);
                        
                        if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + @"\Programme\HomeState\"))
                        {
                            Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + @"\Programme\HomeState\");
                            appShortcutToStartmenu("HomeState Launcher");
                        }  
                        
                        Process.Start(_installDir + "\\Launcher.exe");
                        Environment.Exit(0);
                    }
                }

                Console.WriteLine("Initialized. Ready to check Launcher Version.");
                if (useCurrentPath) _installDir = Environment.CurrentDirectory + "\\";
            }
            catch (Exception eex)
            {
                Console.WriteLine(eex);
                Console.Read();
            }
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            var version = _updateManifest.LauncherVersion;

            var launcherFile = _installDir + "launcher\\HomeState.Launcher.exe";
            if (File.Exists(launcherFile))
            {
                var fvi = FileVersionInfo.GetVersionInfo(launcherFile);
                if (fvi.FileVersion != version || _forceUpdate)
                {
                    Console.WriteLine("Installed Version is " + fvi.FileVersion);
                    Console.WriteLine("Origin Version is " + version);
                    Console.Read();
                    if(_forceUpdate) Console.WriteLine("Forced redownloading all files from commandline.");
                    
                    
                    
                    DoUpdate();
                }
                else
                {
                    
                    StartLauncher();
                }
            }
            else
            {
                DoUpdate();
            }
        }

        private void appShortcutToStartmenu(string linkName)
        {
            string programmDir = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + @"\Programme\HomeState\";
            string deskDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            using (StreamWriter writer = new StreamWriter(programmDir + "\\" + linkName + ".url"))
            {
                string app = _installDir + "Launcher.exe";
                writer.WriteLine("[InternetShortcut]");
                writer.WriteLine("URL=file:///" + app);
                writer.WriteLine("IconIndex=0");
                string icon = app.Replace('\\', '/');
                writer.WriteLine("IconFile=" + icon);
                writer.Flush();
            }
            
            using (StreamWriter writer = new StreamWriter(deskDir + "\\" + linkName + ".url"))
            {
                string app = _installDir + "Launcher.exe";
                writer.WriteLine("[InternetShortcut]");
                writer.WriteLine("URL=file:///" + app);
                writer.WriteLine("IconIndex=0");
                string icon = app.Replace('\\', '/');
                writer.WriteLine("IconFile=" + icon);
                writer.Flush();
            }
        }
        
        public void StartLauncher()
        {
            var p = new Process
            {
                StartInfo =
                {
                    FileName = _installDir + "\\launcher\\HomeState.Launcher.exe",
                    WorkingDirectory = _installDir + "\\launcher\\",
                    Verb = "runas"
                }
            };
            p.Start();

            Environment.Exit(0);
        }

        public void DoUpdate()
        {
            Console.WriteLine("Downloading Update for Launcher");
            if (Directory.Exists(_installDir + "\\launcher\\")) Directory.Delete(_installDir + "\\launcher\\", true);
            _webClient.DownloadProgressChanged += (sender, args) =>
            {
                Dispatcher.Invoke(() => { UpdateProgress.Value = args.ProgressPercentage; });
                Dispatcher.Invoke(() =>
                {

                    var mbDownloaded = ConvertBytesToMegabytes(args.BytesReceived);
                    var mbToDownload = ConvertBytesToMegabytes(args.TotalBytesToReceive);
                    
                    progressLabel.Content = $"{args.ProgressPercentage}% ({(int)mbDownloaded} MB / {(int)mbToDownload} MB)";
                });
            };
            _webClient.DownloadFileCompleted += (sender, args) =>
            {
                new Task(() =>
                {
                    Dispatcher.Invoke(() => statusLabel.Content = "Launcher wird installiert...");
                    ZipFile.ExtractToDirectory(_installDir + "\\TEMP\\Update.zip", _installDir + "\\launcher\\");

                    Directory.Delete(_installDir + "\\TEMP\\", true);
                    
                    StartLauncher();
                    
                }).Start();
                
            };

            if (Directory.Exists(_installDir + "\\TEMP\\"))
            {
                Directory.Delete(_installDir + "\\TEMP\\", true);
            }

            Directory.CreateDirectory(_installDir + "\\TEMP\\");

            if (File.Exists(_installDir + "\\TEMP\\Update.zip")) File.Delete(_installDir + "\\TEMP\\Update.zip");

            _webClient.DownloadFileAsync(new Uri(_updateManifest.LauncherArchive), _installDir + "\\TEMP\\Update.zip");
            Dispatcher.Invoke(() => statusLabel.Content = "Launcher wird heruntergeladen...");
        }


        private static double ConvertBytesToMegabytes(long bytes)
        {
            return (bytes / 1024f) / 1024f;
        }
        
        public static bool HasFolderWritePermission(string destDir)
        {
            if (string.IsNullOrEmpty(destDir) || !Directory.Exists(destDir)) return false;
            try
            {
                var security = Directory.GetAccessControl(destDir);
                var users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                foreach (AuthorizationRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
                {
                    if (rule.IdentityReference == users)
                    {
                        var rights = (FileSystemAccessRule) rule;
                        if (rights.AccessControlType == AccessControlType.Allow)
                        {
                            if (rights.FileSystemRights == (rights.FileSystemRights | FileSystemRights.Modify))
                                return true;
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsElevated => WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid);
    }
}