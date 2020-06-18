using System.IO;

namespace HomeState.Launcher.API
{
    public class Bundle
    {
        public string Name { get; set; }
        public string File { get; set; }
        public Stream FileStream { get; set; }
    }
}