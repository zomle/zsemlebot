using System.Collections.Generic;
using System.Linq;
using System.Threading;
using zsemlebot.core.Domain;
using zsemlebot.core.Extensions;
using zsemlebot.core.Log;
using zsemlebot.repository;
using zsemlebot.twitch;

namespace zsemlebot.services.Commands
{
	public class StreakCommand : TwitchCommand
	{
		public override string Command {get { return Constants.Command_Streak; } }

		public StreakCommand(TwitchService twitchService, HotaService hotaService) 
			: base(twitchService, hotaService)
		{
		}

		protected override void HandleCore(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			if (string.IsNullOrEmpty(parameters))
			{
				//check for current channel
				HandleStreakCommandQueryCurrentChannel(sourceMessageId, channel, sender, parameters);
			}
			else
			{
				//check for user in parameters
				HandleStreakCommandQueryOtherUser(sourceMessageId, channel, sender, parameters);
			}
		}

		private void HandleStreakCommandQueryCurrentChannel(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			var queriedHotaUsers = ListHotaUsers(channel, null);

			HandleStreakCommandForHotaUsers(sourceMessageId, channel, channel[1..], queriedHotaUsers.Item2);
		}

		private void HandleStreakCommandQueryOtherUser(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			var queriedUser = parameters;

			var queriedHotaUsers = new List<HotaUser>();
			var twitchUser = TwitchRepository.GetUser(queriedUser);
			if (twitchUser != null)
			{
				//supplied parameter is an existing twitch user;
				var links = BotRepository.GetLinksForTwitchUser(twitchUser);
				queriedHotaUsers.AddRange(links.LinkedHotaUsers);
			}

			var hotaUser = HotaRepository.GetUser(queriedUser);
			if (hotaUser != null && !queriedHotaUsers.Any(hu => hu.HotaUserId == hotaUser.HotaUserId))
			{
				queriedHotaUsers.Add(hotaUser);
			}

			HandleStreakCommandForHotaUsers(sourceMessageId, channel, parameters, queriedHotaUsers);
		}

		private void HandleStreakCommandForHotaUsers(string? sourceMessageId, string channel, string queriedUser, IReadOnlyList<HotaUser> hotaUsers)
		{
			if (hotaUsers.Count == 0)
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.UserNotFound(queriedUser));
				return;
			}

			new Thread(() =>
			{
				HotaService.RequestGameHistoryAndWait(hotaUsers);

				var relevantGames = new List<HotaUserGameHistoryEntry>();

				var gameList = new List<HotaUserGameHistoryEntry>();
				var mainUserIds = new HashSet<uint>();
				foreach (var oldUserData in hotaUsers)
				{
					mainUserIds.Add(oldUserData.HotaUserId);

					var hotaUser = HotaRepository.GetUser(oldUserData.HotaUserId);
					if (hotaUser == null)
					{
						continue;
					}

					foreach (var gameEntry in hotaUser.GameHistory.Values)
					{
						if (gameEntry.OutCome == 1)
						{
							continue;
						}

						gameList.Add(gameEntry);
					}
				}

				int streak = 0;
				bool? winStreak = null;

				foreach (var game in gameList.OrderByDescending(g => g.GameTimeInUtc))
				{
					bool currentGameWon;
					if (mainUserIds.Contains(game.Player1.UserId))
					{
						currentGameWon = game.Player1.EloChange > game.Player2.EloChange;
					}
					else if (mainUserIds.Contains(game.Player2.UserId))
					{
						currentGameWon = game.Player2.EloChange > game.Player1.EloChange;
					}
					else
					{
						BotLogger.Instance.LogEvent(BotLogSource.Intrnl, $"ERROR. Neither player in the game history belongs to the current hota user. Game id: {game.GameId.ToHexString()}; Player1 id: {game.Player1.UserId.ToHexString()}; Player2 id: {game.Player2.UserId.ToHexString()}; Requested hota user ids: {string.Join("; ", mainUserIds.Select(uid => uid.ToHexString()))}");
						continue;
					}

					if (winStreak == null)
					{
						winStreak = currentGameWon;
						streak++;
					}
					else
					{
						if (winStreak == currentGameWon)
						{
							streak++;
						}
						else
						{
							break;
						}
					}
				}

				if (streak == 0 || winStreak == null)
				{
					var message = MessageTemplates.StreakNoGamesFound(queriedUser);
					TwitchService.SendChatMessage(sourceMessageId, channel, message);
				}
				else if (streak == 1)
				{
					var message = MessageTemplates.StreakNoStreak(queriedUser, winStreak.Value);
					TwitchService.SendChatMessage(sourceMessageId, channel, message);
				}
				else
				{
					var message = MessageTemplates.Streak(queriedUser, winStreak.Value, streak);
					TwitchService.SendChatMessage(sourceMessageId, channel, message);
				}
			}).Start();
		}
	}
}
