namespace HomeState.Updater
{
    public class UpdateManifest
    {
        public string UpdaterVersion { get; set; }
        public string UpdaterHash { get; set; }
        public string UpdaterFile { get; set; }
        public string LauncherVersion { get; set; }
        public string LauncherHash { get; set; }
        public string LauncherArchive { get; set; }
    }
}
