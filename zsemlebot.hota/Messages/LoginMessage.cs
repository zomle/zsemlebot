namespace zsemlebot.hota.Messages
{
    public partial class LoginMessage : HotaMessageBase
    {
        public string User { get; set; }
        public string Password { get; set; }
        public uint LobbyClientVersion { get; set; }

        public LoginMessage(string user, string password, uint lobbyClientVersion)
        {
            User = user;
            Password = password;
            LobbyClientVersion = lobbyClientVersion;
        }

        //The implementation of `byte[] AsByteArray()` is kept in a separate file, that is not commited to git.
        //The reason for this is to not to make it easy for potential bad actors to abuse the hota lobby.
    }
}
