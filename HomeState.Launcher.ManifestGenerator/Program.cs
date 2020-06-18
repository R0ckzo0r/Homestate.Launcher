using HomeState.Updater;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml.Serialization;

namespace HomeState.Launcher.ManifestGenerator
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            //TODO PFAD ANGEBEN BEI UPDATE
            var updateApp = @"C:\Users\Daniel\RiderProjects\Launcher\HomeState.Updater\bin\x64\Release\HomeState.Updater.exe";
            var launcherApp = @"C:\Users\Daniel\RiderProjects\Launcher\HomeState.Launcher\bin\x64\Release\HomeState.Launcher.exe";
            
            var updaterAppInfo = FileVersionInfo.GetVersionInfo(updateApp);
            var launcherAppInfo = FileVersionInfo.GetVersionInfo(launcherApp);


            var manifest = new UpdateManifest
            {
                UpdaterVersion = updaterAppInfo.FileVersion,
                UpdaterHash = BytesToString(GetHashSha256(updateApp)),
                UpdaterFile = "http://launcher.homestate.de/Launcher.exe",
                LauncherVersion = launcherAppInfo.FileVersion,
                LauncherHash = BytesToString(GetHashSha256(launcherApp)),
                LauncherArchive = "http://launcher.homestate.de/Update.zip"
            };

            var xs = new XmlSerializer(typeof(UpdateManifest));  
  
            TextWriter txtWriter = new StreamWriter(Environment.CurrentDirectory + "\\update.xml");  
  
            xs.Serialize(txtWriter, manifest);  
  
            txtWriter.Close();  


        }
        
        private static SHA256 Sha256 = SHA256.Create();

        public static byte[] GetHashSha256(string filename)
        {
            using (FileStream stream = File.OpenRead(filename))
            {
                return Sha256.ComputeHash(stream);
            }
        }
        
        public static string BytesToString(byte[] bytes)
        {
            string result = "";
            foreach (byte b in bytes) result += b.ToString("x2");
            return result;
        }
        
    }


   


}