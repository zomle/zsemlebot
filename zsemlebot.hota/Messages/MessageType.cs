﻿namespace zsemlebot.hota.Messages
{
    public enum MessageType
    {
        // client -> server messages
        Login = 0x83,
        SendChatMessage = 0x47,
        RequestUserRep = 0x68,
        RequestUserElo = 0x74,
        MaybePing = 0x95,
		RequestGameHistory = 0x97,
		RequestMapInfo = 0x99,
		ExitLobby = 0x52
	}
}
