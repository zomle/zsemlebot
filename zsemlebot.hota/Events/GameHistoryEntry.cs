using System;
using zsemlebot.core.Enums;

namespace zsemlebot.hota.Events
{
	public class GameHistoryEntry
	{
		public uint GameId { get; }
		public uint MapId { get; }
		public DateTime GameTimeInUtc { get; }
		public int OutCome { get; }

		public uint Player1UserId { get; }
		public HotaTown Player1Town { get; }
		public HotaColor Player1Color { get; }
		public byte Player1Hero { get; }
		public int Player1OldElo { get; }
		public int Player1NewElo { get; }

		public uint Player2UserId { get; }
		public HotaTown Player2Town { get; }
		public HotaColor Player2Color { get; }
		public byte Player2Hero { get; }
		public int Player2OldElo { get; }
		public int Player2NewElo { get; }

		public GameHistoryEntry(uint gameId, uint mapId, DateTime gameTimeInUtc, int outCome, 
			uint player1UserId, byte player1Color, byte player1Town, byte player1Hero, int player1OldElo, int player1NewElo, 
			uint player2UserId, byte player2Color, byte player2Town, byte player2Hero, int player2OldElo, int player2NewElo)
		{
			GameId = gameId;
			MapId = mapId;
			GameTimeInUtc = gameTimeInUtc;
			OutCome = outCome;
			
			Player1UserId = player1UserId;
			Player1Town = (HotaTown)player1Town;
			Player1Color = (HotaColor)player1Color;
			Player1Hero = player1Hero;
			Player1OldElo = player1OldElo;
			Player1NewElo = player1NewElo;

			Player2UserId = player2UserId;
			Player2Town = (HotaTown)player2Town;
			Player2Color = (HotaColor)player2Color;
			Player2Hero = player2Hero;
			Player2OldElo = player2OldElo;
			Player2NewElo = player2NewElo;
		}
	}
}
