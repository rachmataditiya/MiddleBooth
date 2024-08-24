namespace MiddleBooth.Services
{
    public class OdooConnection
    {
        public string ServerUrl { get; }
        public string Database { get; }
        public string Username { get; }
        public string Password { get; }

        public OdooConnection(string serverUrl, string database, string username, string password)
        {
            ServerUrl = serverUrl;
            Database = database;
            Username = username;
            Password = password;
        }
    }
}