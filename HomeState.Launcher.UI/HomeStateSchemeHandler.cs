using System;
using System.IO;
using System.Linq;
using System.Net;
using CefSharp;
using NLog;

namespace HomeState.Launcher.UI
{
public class HomeStateSchemeHandler : ISchemeHandlerFactory
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            
            if (schemeName == "hs")
            {
                var baseDir = Environment.CurrentDirectory + "\\plugins\\ui\\html\\";
                var str = request.Url.Replace("hs://res/", "").Split('?')[0];
                
                
                var filePath = str;
                
                try
                {
                    var mimetype = "";

                    switch (str.Split('.').Last())
                    {
                        case "html":
                        {
                            mimetype = "text/html";
                            break;
                        }
                        case "css":
                        {
                            mimetype = "text/css";
                            break;
                        }
                        case "js":
                        {
                            mimetype = "text/javascript";
                            break;
                        }
                        default:
                        {
                            mimetype = "application/octet-stream";
                            break;
                        }
                    }

                    //return ResourceHandler.FromFilePath(filePath, mimetype);

                    var bundleName = str.Split('/').First();

                    return ResourceHandler.FromStream(
                        UIPlugin.PluginManager.GetResourceManager().GetFileStream(bundleName, filePath.Replace(bundleName + "/", "")), mimetype);
                }
                catch (FileNotFoundException e)
                {
                    _logger.Warn(e);
                    return ResourceHandler.ForErrorMessage("File not found.", HttpStatusCode.NotFound);
                    
                }

                //return ResourceHandler.ForErrorMessage("File not found.", HttpStatusCode.NotFound);

            }
            
            return new ResourceHandler();
        }
    }
}