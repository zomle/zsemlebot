using System.Collections.Generic;
using zsemlebot.core.Domain;
using zsemlebot.core.Enums;
using zsemlebot.services.Log;

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

            game.JoinedUsers.Add(user);

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

            game.JoinedUsers.RemoveAll(ju => ju.HotaUserId == user.HotaUserId);

            //if (game.MaxPlayerCount == game.JoinedUsers.Count + 1)
            //{
            //    NotFullCount++;
            //    NotStartedCount--;
            //}

            if (ActiveGames.Count != InProgressCount + NotStartedCount + NotFullCount)
            {
                BotLogger.Instance.LogEvent(BotLogSource.Hota, $"Active game count ({ActiveGames.Count}) not equal to InProgressCount ({InProgressCount}) + NotStartedCount ({NotStartedCount}) + NotFullCount ({NotFullCount})");
            }
        }
    }
}