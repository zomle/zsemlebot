namespace zsemlebot.hota.Messages
{
    public partial class LoginMessage : HotaMessageBase
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public uint LobbyClientVersion { get; set; }

        //The implementation of `byte[] AsByteArray()` is kept in a separate file, that is not commited to git.
        //The reason for this is to not to make it easy for potential bad actors to abuse the hota lobby.
    }
}
