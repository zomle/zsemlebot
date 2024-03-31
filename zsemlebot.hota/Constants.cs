using System;

namespace zsemlebot.hota
{
	internal static class Constants
	{
		public const byte ChatMessageDstType_PublicLobby = 0;
		public const byte ChatMessageDstType_PrivateMessage = 1;

		public const short MsgTypex31_AuthReply = 0x31;
		public const short MsgTypex33_UserJoinedLobby = 0x33;
		public const short MsgTypex34_OwnInfo = 0x34;
		public const short MsgTypex36_UserStatusChange = 0x36;
		public const short MsgTypex38_GameRoomItem = 0x38;
		public const short MsgTypex39_GameUserChange = 0x39;
		public const short MsgTypex3A_GameEnded = 0x3A;
		public const short MsgTypex46_OldChatMessage = 0x46;
		public const short MsgTypex47_NewChatMessage = 0x47;
		public const short MsgTypex53_UserLeftLobby = 0x53;
		public const short MsgTypex69_UserRepUpdate = 0x69;
		public const short MsgTypex6B_UserLeftLobby2 = 0x6B;
		public const short MsgTypex6C_UnknownUserEvent = 0x6C; //probably left game?
		public const short MsgTypex72_SuccessfulLogin = 0x72;
		public const short MsgTypex75_UserEloUpdate = 0x75;
		public const short MsgTypex7E_NewDonation = 0x7E;
		public const short MsgTypex7F_DonationGoal = 0x7F;
		public const short MsgTypex80_Donations = 0x80;
		public const short MsgTypex85_Unknown1 = 0x85;
		public const short MsgTypex8A_Unknown2 = 0x8A;
		public const short MsgTypex8C_GameStarted = 0x8C; //game starts
		public const short MsgTypex98_RankedGameHistory = 0x98;
		public const short MsgTypex9A_MapInfo = 0x9A;
	}
}
