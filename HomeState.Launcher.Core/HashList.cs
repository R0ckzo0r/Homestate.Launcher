    using System.Collections.Generic;


namespace HomeState.Launcher.Core
{

    class HashList
    {
        public List<File> files { get; set; }
    }

    class File
    {
        public string name { get; set; }
        public string hash { get; set; }
    }
}