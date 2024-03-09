using System;

namespace zsemlebot.core.Domain
{
	public class HotaUserGameHistoryEntry
	{
		public uint GameId { get; }
		public DateTime GameTimeInUtc { get; }
		public int OutCome { get; }

		public uint Player1UserId { get; }
		public int Player1OldElo { get; }
		public int Player1NewElo { get; }
		public int Player1EloChange { get { return Player1NewElo - Player1OldElo; } }

		public uint Player2UserId { get; }
		public int Player2OldElo { get; }
		public int Player2NewElo { get; }
		public int Player2EloChange { get { return Player2NewElo - Player2OldElo; } }

		public HotaUserGameHistoryEntry(uint gameId, DateTime gameTimeInUtc, int outCome,
			uint player1Id, int player1OldElo, int player1NewElo,
			uint player2Id, int player2OldElo, int player2NewElo)
		{
			GameId = gameId;
			GameTimeInUtc = gameTimeInUtc;
			OutCome = outCome;
			Player1UserId = player1Id;
			Player1OldElo = player1OldElo;
			Player1NewElo = player1NewElo;
			Player2UserId = player2Id;
			Player2OldElo = player2OldElo;
			Player2NewElo = player2NewElo;
		}
	}
}
