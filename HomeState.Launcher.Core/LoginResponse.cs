namespace HomeState.Launcher.Core
{
    public class LoginResponse
    {
        public int StatusCode { get; set; }
        public User UserData { get; set; }
        public string LauncherVersion { get; set; }
    }
    public class User
    {
        public string Avatar { get; set; }
        public string UserName { get; set; }
        public string AuthCode { get; set; }
        public string UpdateUrl { get; set; }
        public string GameAddress { get; set; }
    }
}