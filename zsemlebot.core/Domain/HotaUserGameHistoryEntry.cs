using System;
using zsemlebot.core.Enums;

namespace zsemlebot.core.Domain
{

	public class HotaUserGameHistoryEntry
	{
		public uint GameId { get; }
		public DateTime GameTimeInUtc { get; }
		public int OutCome { get; }

		public HotaUserGameHistoryPlayer Player1 { get; }
		public HotaUserGameHistoryPlayer Player2 { get; }

		public HotaUserGameHistoryEntry(uint gameId, DateTime gameTimeInUtc, int outCome,
			uint player1Id, HotaColor player1Color, HotaTown player1Town, byte player1Hero, int player1OldElo, int player1NewElo,
			uint player2Id, HotaColor player2Color, HotaTown player2Town, byte player2Hero, int player2OldElo, int player2NewElo)
		{
			GameId = gameId;
			GameTimeInUtc = gameTimeInUtc;
			OutCome = outCome;

			Player1 = new HotaUserGameHistoryPlayer
			{
				UserId = player1Id,
				Color = player1Color,
				Town = player1Town,
				Hero = player1Hero,
				OldElo = player1OldElo,
				NewElo = player1NewElo,
			};

			Player2 = new HotaUserGameHistoryPlayer
			{
				UserId = player2Id,
				Color = player2Color,
				Town = player2Town,
				Hero = player2Hero,
				OldElo = player2OldElo,
				NewElo = player2NewElo,
			};
		}
	}
}
