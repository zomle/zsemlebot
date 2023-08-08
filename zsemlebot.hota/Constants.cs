using System;

namespace zsemlebot.hota
{
    internal static class Constants
    {
        public const byte ChatMessageDstType_PublicLobby = 0;
        public const byte ChatMessageDstType_PrivateMessage = 1;

        public const short MsgType_AuthReply = 0x31;
        public const short MsgType_UserJoinedLobby = 0x33;
        public const short MsgType_OwnInfo = 0x34;
        public const short MsgType_UserStatusChange = 0x36;
        public const short MsgType_GameRoomItem = 0x38;
        public const short MsgType_GameUserChange = 0x39;
        public const short MsgType_GameEnded = 0x3A;
        public const short MsgType_OldChatMessage = 0x46;
        public const short MsgType_NewChatMessage = 0x47;
        public const short MsgType_UserLeftLobby = 0x53;
        public const short MsgType_UserLeftLobby2 = 0x6B;
        public const short MsgType_UnknownUserEvent = 0x6C; //probably left game?
        public const short MsgType_SuccessfulLogin = 0x72;
        public const short MsgType_DonationGoal = 0x7F;
        public const short MsgType_Donators = 0x80;
        public const short MsgType_Unknown1 = 0x85;
        public const short MsgType_Unknown2 = 0x8A;
        public const short MsgType_GameStatusChange = 0x8C; //game status change
    }
}
