namespace MiddleBooth.Services
{
    public class OdooConnection(string serverUrl, string database, string username, string password)
    {
        public string ServerUrl { get; } = serverUrl;
        public string Database { get; } = database;
        public string Username { get; } = username;
        public string Password { get; } = password;
    }
}