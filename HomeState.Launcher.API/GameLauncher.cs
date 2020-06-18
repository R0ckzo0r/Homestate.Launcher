namespace HomeState.Launcher.API
{
    public interface GameLauncher
    {
        string GetGamePath { get; }
        void StartGame(string address, int port, string gamePath, GTAVVersion type);
    }
    public enum GTAVVersion
    {
        Steam,
        SocialClub
    }
}