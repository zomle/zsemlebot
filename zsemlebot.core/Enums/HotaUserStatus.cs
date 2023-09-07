using System;

namespace zsemlebot.core.Enums
{
    public enum HotaUserStatus
    {
        InLobby = 1,
        Unknown1 = 2,
        InNewGameRoom = 3,
        InLoadRoom = 4,
        InGame = 5,
        Unknown2 = 6,

        Offline = byte.MaxValue
    }
}
