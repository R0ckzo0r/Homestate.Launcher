using System.Collections.Generic;
using System.IO;
using System.Linq;
using HomeState.Launcher.API;
using HomeState.Launcher.Exception;
using Ionic.Zip;
using NLog;

namespace HomeState.Launcher
{
    public class ResourceManager : IResourceManager
    {
        private Dictionary<string, string> _bundleFileIndex;
        private List<Bundle> _registeredBundles;
        
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        
        public ResourceManager()
        {
            _bundleFileIndex = new Dictionary<string, string>();
            _registeredBundles = new List<Bundle>();
        }

        public void RegisterBundle(string file)
        {
            if (File.Exists(file))
            { 
                _logger.Info("Loading Bundle " + file);
                using (var bundle = ZipFile.Read(file))
                {
                    var bundleName = new FileInfo(file).Name;
                    
                    foreach (var item in bundle)
                    {
                        if (item.IsDirectory) continue;
                        _bundleFileIndex.Add($"{bundleName}/{item.FileName}", bundleName);
                    }
                    
                    _registeredBundles.Add(new Bundle
                    {
                        Name = bundleName,
                        File = file,
                        FileStream = new FileStream(file, FileMode.Open)
                    });
                }
            }
            else
            {
                throw new FileNotFoundException();
            }
        }

        public Stream GetFileStream(string bundleStr, string file)
        {
            if (_bundleFileIndex.ContainsKey($"{bundleStr}/{file}"))
            {
                if (_registeredBundles.FirstOrDefault(x => x.Name == bundleStr) != null)
                {
                    var bundle = _registeredBundles.FirstOrDefault(x => x.Name == bundleStr);
                    if (bundle == null) throw new BundleNotFoundException();
                    _logger.Debug($"[{bundle?.Name}] Loading file {file}");
                    bundle?.FileStream.Seek(0, SeekOrigin.Begin);
                    using (var bundleZip = ZipFile.Read(bundle?.FileStream))
                    {
                        var fStream = new MemoryStream();
                        if (bundleZip[file] == null) throw new FileNotFoundException();
                        bundleZip[file].Extract(fStream);
                        
                        return fStream;
                    }
                }
                throw new FileNotFoundException();
            }

            _logger.Warn($"File \"{file}\" is not in index.");
            throw new FileNotFoundException();
        }

        public byte[] GetFileByteArray(string bundle, string file)
        {
            return ReadFully(GetFileStream(bundle, file));
        }
        
        private static byte[] ReadFully(Stream input)
        {
            using (var ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}