using System.IO;

namespace HomeState.Launcher.API
{
    public interface IResourceManager
    {
        void RegisterBundle(string file);
        Stream GetFileStream(string bundle, string file);
        byte[] GetFileByteArray(string bundle, string file);
    }
}