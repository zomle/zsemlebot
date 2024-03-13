﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using zsemlebot.core.Domain;
using zsemlebot.core.Extensions;
using zsemlebot.repository;
using zsemlebot.services.Log;
using zsemlebot.twitch;

namespace zsemlebot.services.Commands
{
	public class TodayCommand : TwitchCommand
	{
		public override string Command {get { return Constants.Command_Today; } }

		public TodayCommand(TwitchService twitchService, HotaService hotaService) 
			: base(twitchService, hotaService)
		{
		}

		protected override void HandleCore(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			if (string.IsNullOrEmpty(parameters))
			{
				//check for current channel
				HandleTodayCommandQueryCurrentChannel(sourceMessageId, channel, sender, parameters);
			}
			else
			{
				//check for user in parameters
				HandleTodayCommandQueryOtherUser(sourceMessageId, channel, sender, parameters);
			}
		}

		private void HandleTodayCommandQueryCurrentChannel(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			var (twitchUser, queriedHotaUsers) = ListHotaUsers(channel, null);

			var offset = TimeSpan.Zero;
			if (twitchUser != null)
			{
				var settings = BotRepository.ListZsemlebotSettings(p => p.ChannelTwitchUserId == twitchUser.TwitchUserId && p.SettingName == Constants.Settings_TimeZone);
				var setting = settings.FirstOrDefault();
				if (setting != null)
				{
					offset = GetTimeOffset(setting.SettingValue ?? "+00:00");
				}
			}

			HandleTodayCommandForHotaUsers(sourceMessageId, channel, channel[1..], queriedHotaUsers, offset);
		}

		private void HandleTodayCommandQueryOtherUser(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
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

			var offset = TimeSpan.Zero;
			if (twitchUser != null)
			{
				var settings = BotRepository.ListZsemlebotSettings(p => p.ChannelTwitchUserId == twitchUser.TwitchUserId && p.SettingName == Constants.Settings_TimeZone);
				var setting = settings.FirstOrDefault();
				if (setting != null)
				{
					offset = GetTimeOffset(setting.SettingValue ?? "+00:00");
				}
			}

			HandleTodayCommandForHotaUsers(sourceMessageId, channel, parameters, queriedHotaUsers, offset);
		}

		private TimeSpan GetTimeOffset(string value)
		{
			var pm = value[0];
			var hours = int.Parse(value[1..3]);
			var mins = int.Parse(value[4..6]);

			hours = (pm == '+' ? 1 : -1) * hours;

			return new TimeSpan(hours, mins, 0);
		}

		private void HandleTodayCommandForHotaUsers(string? sourceMessageId, string channel, string queriedUser, IReadOnlyList<HotaUser> hotaUsers, TimeSpan timeZoneOffset)
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

				int wins = 0;
				int losses = 0;
				int eloChange = 0;
				int accountCount = 0;
				int currentElo = 0;

				var currentDate = (DateTime.UtcNow + timeZoneOffset).Date;

				foreach (var oldUserData in hotaUsers)
				{
					var hotaUser = HotaRepository.GetUser(oldUserData.HotaUserId);
					if (hotaUser == null)
					{
						continue;
					}

					int relevantGamesBefore = relevantGames.Count;
					foreach (var gameEntry in hotaUser.GameHistory.Values.OrderBy(g => g.GameTimeInUtc))
					{
						if ((gameEntry.GameTimeInUtc + timeZoneOffset).Date != currentDate)
						{
							continue;
						}

						if (gameEntry.OutCome == 1)
						{
							continue;
						}

						relevantGames.Add(gameEntry);

						if (gameEntry.Player1UserId == hotaUser.HotaUserId)
						{
							eloChange += gameEntry.Player1EloChange;
							currentElo = gameEntry.Player1NewElo;

							if (gameEntry.Player1EloChange > gameEntry.Player2EloChange)
							{
								wins++;
							}
							else
							{
								losses++;
							}
						}
						else if (gameEntry.Player2UserId == hotaUser.HotaUserId)
						{
							eloChange += gameEntry.Player2EloChange;
							currentElo = gameEntry.Player2NewElo;

							if (gameEntry.Player2EloChange > gameEntry.Player1EloChange)
							{
								wins++;
							}
							else
							{
								losses++;
							}
						}
						else
						{
							BotLogger.Instance.LogEvent(BotLogSource.Intrnl, $"ERROR. Neither player in the game history belongs to the current hota user. Game id: {gameEntry.GameId.ToHexString()}; Player1 id: {gameEntry.Player1UserId.ToHexString()}; Player2 id: {gameEntry.Player2UserId.ToHexString()}; Requested hota user id: {hotaUser.HotaUserId.ToHexString()}");
						}
					}

					if (relevantGames.Count > relevantGamesBefore)
					{
						accountCount++;
					}
				}

				if (relevantGames.Count == 0)
				{
					//no game history
					var message = MessageTemplates.HistoryTodayNoInfo(queriedUser);
					TwitchService.SendChatMessage(sourceMessageId, channel, message);
				}
				else if (accountCount > 1)
				{
					//multiple accounts are aggregated
					var message = MessageTemplates.HistoryTodayMultipleAccount(queriedUser, wins, losses, eloChange, accountCount);
					TwitchService.SendChatMessage(sourceMessageId, channel, message);
				}
				else
				{
					//single account
					var message = MessageTemplates.HistoryTodaySingleAccount(queriedUser, wins, losses, eloChange, currentElo);
					TwitchService.SendChatMessage(sourceMessageId, channel, message);
				}
			}).Start();
		}

	}
}