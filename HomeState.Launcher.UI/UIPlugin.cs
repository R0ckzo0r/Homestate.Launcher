using CefSharp;
using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp.Enums;
using CefSharpDraggableRegion;
using HomeState.Launcher.API;
using NLog;
using TestUI;
#pragma warning disable 618


namespace HomeState.Launcher.UI
{
    public class UIPlugin : IPlugin
    {
        public ChromiumWebBrowser ChromeBrowser;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public static IPluginManager PluginManager;
        public static UIPlugin Instance;

        public delegate void UILoadedHandler(object sender);
        public delegate void UILoadingHandler(object sender);

        public event UILoadedHandler OnUILoaded;
        public event UILoadedHandler OnUILoading;

        public string OverrideUrl = String.Empty;

        public Form Window;

        const int WM_MOUSEMOVE = 0x0200;
        const int WM_MOUSELEAVE = 0x02A3;
        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_LBUTTONUP = 0x0202;

        public void OnReady()
        {
            ThreadPool.QueueUserWorkItem((e) =>
            {
                Application.EnableVisualStyles();
                //Application.SetCompatibleTextRenderingDefault(false);

                SetProcessDPIAware();
                _logger.Info("Starting UI...");
                
                Window = new UIWindow();
                
              
                
                Window.Text = "Test";
                Window.BackColor = Color.FromArgb(42, 42, 42);

                try
                {
                    // window.Icon = new Icon(PluginManager.GetResourceManager().GetFileStream("ui.bundle", "eee.png"), 32,32);
                }
                catch (Exception iconLoadError)
                {
                    _logger.Warn(iconLoadError);
                }

                CefSettings settings = new CefSettings();

                settings.RegisterScheme(new CefCustomScheme
                {
                    SchemeName = "hs",
                    SchemeHandlerFactory = new HomeStateSchemeHandler(),
                    IsCorsEnabled = false
                });
               
                settings.CefCommandLineArgs.Add("disable-features", "CrossSiteDocumentBlockingAlways,CrossSiteDocumentBlockingIfIsolating");
                
                settings.LogSeverity = LogSeverity.Disable;
                Window.Icon = TestUI.Properties.Resources.favicon;
                settings.BackgroundColor = Cef.ColorSetARGB(0, 42, 42, 42);
                settings.Locale = "de";
            
                // Initialize cef with the provided settings
                Cef.Initialize(settings);
                // Create a browser component
                ChromeBrowser = new ChromiumWebBrowser();
                ChromeBrowser.LifeSpanHandler = new SampleLifeSpanHandler();
                CefSharpSettings.LegacyJavascriptBindingEnabled = true;
                OnUILoading?.Invoke(this);

                //ChromeBrowser.BrowserSettings.BackgroundColor = Cef.ColorSetARGB(0, 42, 42, 42);
                ChromeBrowser.BackColor = Color.FromArgb(42, 42, 42);
                
                if(OverrideUrl != String.Empty)
                {
                    ChromeBrowser.Load(OverrideUrl);
                }
                
                ChromeBrowser.BackColor = Color.FromArgb(42, 42, 42);
                ChromeBrowser.MenuHandler = new CustomMenuHandler();
                ChromeBrowser.RegisterJsObject("chrome", new ChromeScriptBridge());
                ChromeBrowser.DragHandler = new DragDropHandler();
                // Add it to the form and fill it to the form window.
                //ChromeBrowser.Size = new System.Drawing.Size(1280, 720);
                ChromeBrowser.Dock = DockStyle.Fill;
                ChromeBrowser.Top = 0;
                ChromeBrowser.Left = 0;
                Window.Controls.Add(ChromeBrowser);
                Window.Text = "HomeState Launcher";

                ChromeBrowser.IsBrowserInitializedChanged += ChromeBrowser_IsBrowserInitializedChanged;

                Window.FormBorderStyle = FormBorderStyle.None;
                Window.StartPosition = FormStartPosition.CenterScreen;
                
                Application.Run(Window);
            });
        }

        public void OnDestroy()
        {
            Cef.Shutdown();
            _logger.Info("Cef Shut down.");
        }

        public void OnLoad(IPluginManager pluginManager)
        {
            PluginManager = pluginManager;
            Instance = this;
            Application.ApplicationExit += (sender, args) => PluginManager.Exit();
        }

        private Point lastLocation;
        private void ChromeBrowser_IsBrowserInitializedChanged(object sender, IsBrowserInitializedChangedEventArgs e)
        {
            if (e.IsBrowserInitialized) OnUILoaded?.Invoke(this);

            ChromeWidgetMessageInterceptor.SetupLoop(ChromeBrowser, (m) =>
            {
                if (m.Msg == WM_LBUTTONDOWN)
                {               
                    Point point = new Point(m.LParam.ToInt32());
                    lastLocation = point;
                    if (((DragDropHandler) ChromeBrowser.DragHandler).draggableRegion.IsVisible(point))
                    {

                        ReleaseCapture();
                        if(Window.InvokeRequired)
                        {
                            Window.Invoke(new SendHandleMessageDelegate(() => { SendMessage(Window.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); }));
                        }
                        else
                        {
                            SendMessage(Window.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                        }
                        Console.WriteLine("[WM_LBUTTONDOWN]");
                    }
                }
            });

        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        public void SendHandleMessage()
        {
            SendMessage(Window.Handle, WM_NCLBUTTONDOWN, 12, 0);
        }

        public delegate void SendHandleMessageDelegate();

    }

    public class ChromeScriptBridge
    {
        public void ToggleDevTools()
        {

            UIPlugin.Instance.ChromeBrowser.ShowDevTools();

        }
    }

    public class CustomMenuHandler : CefSharp.IContextMenuHandler
    {
        public void OnBeforeContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame,
            IContextMenuParams parameters, IMenuModel model)
        {
            model.Clear();
            
        }

        public bool OnContextMenuCommand(IWebBrowser browserControl, IBrowser browser, IFrame frame,
            IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
        {

            return false;
        }

        public void OnContextMenuDismissed(IWebBrowser browserControl, IBrowser browser, IFrame frame) { }

        public bool RunContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame,
            IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
        {
            return false;
        }
    }
    public class SampleLifeSpanHandler: ILifeSpanHandler
    {
        public bool OnBeforePopup(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl,
            string targetFrameName, WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures,
            IWindowInfo windowInfo, IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser)
        {
            Process.Start(targetUrl);
            noJavascriptAccess = false;
            newBrowser = null;
            return true;
        }

        public void OnAfterCreated(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
            
        }

        public bool DoClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
            return true;
        }

        public void OnBeforeClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
        {
           
        }
    }
}
