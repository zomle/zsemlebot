using System;

namespace zsemlebot.hota.Events
{
	public class GameHistoryEntry
	{
		public uint GameId { get; }
		public DateTime GameTimeInUtc { get; }
		public int OutCome { get; }
		public uint Player1UserId { get; }
		public int Player1OldElo { get; }
		public int Player1NewElo { get; }
		public uint Player2UserId { get; }
		public int Player2OldElo { get; }
		public int Player2NewElo { get; }

		public GameHistoryEntry(uint gameId, DateTime gameTimeInUtc, int outCome, 
			uint player1UserId, int player1OldElo, int player1NewElo, 
			uint player2UserId, int player2OldElo, int player2NewElo)
		{
			GameId = gameId;
			GameTimeInUtc = gameTimeInUtc;
			OutCome = outCome;
			Player1UserId = player1UserId;
			Player1OldElo = player1OldElo;
			Player1NewElo = player1NewElo;
			Player2UserId = player2UserId;
			Player2OldElo = player2OldElo;
			Player2NewElo = player2NewElo;
		}
	}
}
