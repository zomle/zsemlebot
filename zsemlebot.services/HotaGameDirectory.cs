using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using zsemlebot.core.Domain;
using zsemlebot.core.Enums;
using zsemlebot.services.Log;
using static System.Collections.Specialized.BitVector32;

namespace zsemlebot.services
{
	public class HotaGameDirectory
	{
		public int NotFullCount { get; private set; }
		public int NotStartedCount { get; private set; }
		public int InProgressCount { get; private set; }

		private Dictionary<GameKey, HotaGame> ActiveGames { get; }

		public HotaGameDirectory()
		{
			ActiveGames = new Dictionary<GameKey, HotaGame>(1000);
			NotFullCount = 0;
			NotStartedCount = 0;
			InProgressCount = 0;
		}
		public void Reset()
		{
			NotFullCount = 0;
			NotStartedCount = 0;
			InProgressCount = 0;

			ActiveGames.Clear();

			BotLogger.Instance.LogEvent(BotLogSource.Hota, $"Game directory is reset.");
		}

		public HotaGame? FindGame(HotaUser user)
		{
			foreach (var game in ActiveGames.Values)
			{
				if (game.JoinedPlayers.Any(ju => ju.HotaUserId == user.HotaUserId))
				{
					return game;
				}
			}

			return null;
		}

		public HotaGame? GetGame(GameKey key)
		{
			ActiveGames.TryGetValue(key, out var result);
			return result;
		}

		public void AddGame(HotaGame game)
		{
			if (ActiveGames.ContainsKey(game.GameKey))
			{
				GameEnded(game.GameKey);
			}

			InProgressCount++;

			ActiveGames.Add(game.GameKey, game);

			if (ActiveGames.Count != InProgressCount + NotStartedCount + NotFullCount)
			{
				BotLogger.Instance.LogEvent(BotLogSource.Hota, $"Active game count ({ActiveGames.Count}) not equal to InProgressCount ({InProgressCount}) + NotStartedCount ({NotStartedCount}) + NotFullCount ({NotFullCount})");
			}
		}

		public HotaGame? GameStarted(GameKey gameKey)
		{
			if (!ActiveGames.TryGetValue(gameKey, out var game))
			{
				return null;
			}

			InProgressCount++;

			if (ActiveGames.Count != InProgressCount + NotStartedCount + NotFullCount)
			{
				BotLogger.Instance.LogEvent(BotLogSource.Hota, $"Active game count ({ActiveGames.Count}) not equal to InProgressCount ({InProgressCount}) + NotStartedCount ({NotStartedCount}) + NotFullCount ({NotFullCount})");
			}

			return game;
		}

		public HotaGame? GameEnded(GameKey gameKey)
		{
			if (!ActiveGames.TryGetValue(gameKey, out var game))
			{
				return null;
			}

			InProgressCount--;

			game.Status = HotaGameStatus.Finished;

			ActiveGames.Remove(gameKey);

			if (ActiveGames.Count != InProgressCount + NotStartedCount + NotFullCount)
			{
				BotLogger.Instance.LogEvent(BotLogSource.Hota, $"Active game count ({ActiveGames.Count}) not equal to InProgressCount ({InProgressCount}) + NotStartedCount ({NotStartedCount}) + NotFullCount ({NotFullCount})");
			}

			return game;
		}

		public void UserJoin(GameKey gameKey, HotaUser user)
		{
			if (!ActiveGames.TryGetValue(gameKey, out var game))
			{
				return;
			}

			game.JoinedPlayers.Add(new HotaGamePlayer(user));

			if (ActiveGames.Count != InProgressCount + NotStartedCount + NotFullCount)
			{
				BotLogger.Instance.LogEvent(BotLogSource.Hota, $"Active game count ({ActiveGames.Count}) not equal to InProgressCount ({InProgressCount}) + NotStartedCount ({NotStartedCount}) + NotFullCount ({NotFullCount})");
			}
		}

		public void UserLeft(GameKey gameKey, HotaUser user)
		{
			if (!ActiveGames.TryGetValue(gameKey, out var game))
			{
				return;
			}

			game.JoinedPlayers.RemoveAll(ju => ju.HotaUserId == user.HotaUserId);

			if (ActiveGames.Count != InProgressCount + NotStartedCount + NotFullCount)
			{
				BotLogger.Instance.LogEvent(BotLogSource.Hota, $"Active game count ({ActiveGames.Count}) not equal to InProgressCount ({InProgressCount}) + NotStartedCount ({NotStartedCount}) + NotFullCount ({NotFullCount})");
			}
		}

		public void UpdateTemplate(GameKey gameKey, string newTemplateName)
		{
			var game = GetGame(gameKey);
			if (game == null)
			{
				BotLogger.Instance.LogEvent(BotLogSource.Intrnl, $"Couldn't find game by key ({gameKey}) in {nameof(HotaGameDirectory)}.{nameof(UpdateTemplate)}().");
				return;
			}

			game.Template = newTemplateName ?? game.Template;
			BotLogger.Instance.LogEvent(BotLogSource.User, $"Template updated for game ({gameKey}). New template: {newTemplateName}");
		}

		public void UpdatePlayerInfo(GameKey gameKey, HotaGamePlayer player, string? color, string? faction, int? trade)
		{
			var game = GetGame(gameKey);
			if (game == null)
			{
				BotLogger.Instance.LogEvent(BotLogSource.Intrnl, $"Couldn't find game by key ({gameKey}) in {nameof(HotaGameDirectory)}.{nameof(UpdatePlayerInfo)}().");
				return;
			}

			player.Faction = faction ?? player.Faction;
			player.Color = color ?? player.Color;
			player.Trade = trade ?? player.Trade;
			BotLogger.Instance.LogEvent(BotLogSource.User, $"Player info updated for game ({gameKey}). Faction: {faction}; color: {color}; trade: {trade}");
		}
	}
}