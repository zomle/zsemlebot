using System.Collections.Generic;
using System;
using zsemlebot.core.Domain;
using zsemlebot.repository;
using zsemlebot.twitch;
using System.Linq;
using zsemlebot.core.Log;

namespace zsemlebot.services.Commands
{
	public abstract class TwitchCommand
	{
		public abstract string Command { get; }

		protected TwitchService TwitchService { get; }
		protected HotaService HotaService { get; }
		protected BotService? BotService { get; }

		protected BotRepository BotRepository { get { return BotRepository.Instance; } }
		protected TwitchRepository TwitchRepository { get { return TwitchRepository.Instance; } }
		protected HotaRepository HotaRepository { get { return HotaRepository.Instance; } }

		protected TwitchCommand(TwitchService twitchService, HotaService hotaService)
			: this(twitchService, hotaService, null)
		{
		}

		protected TwitchCommand(TwitchService twitchService, HotaService hotaService, BotService? botService)
		{
			TwitchService = twitchService;
			HotaService = hotaService;
			BotService = botService;
		}

		public void Handle(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			bool isUserIgnored = TwitchRepository.IsUserOnIgnoreList(sender.TwitchUserId);
			if (isUserIgnored)
			{
				BotLogger.Instance.LogEvent(BotLogSource.Intrnl, $"User on ignore list tried to use a command. User: {sender.DisplayName} @ {channel}. Command: {Command} {parameters}");
				return;
			}

			if (!sender.IsModOrBroadcaster)
			{
				bool isBotSpammed = TwitchService.IsUserSpammingCommands(channel, sender.DisplayName);
				if (isBotSpammed)
				{
					BotLogger.Instance.LogEvent(BotLogSource.Intrnl, $"User seems to spam the bot: {sender.DisplayName} @ {channel}");
					return;
				}

				TwitchService.RegisterUserCommandUsage(channel, sender.DisplayName);
			}

			if (!IsCommandEnabledForChannel(channel, Command))
			{
				BotLogger.Instance.LogEvent(BotLogSource.Intrnl, $"User tried to use a disabled command. User: {sender.DisplayName} @ {channel}. Command: {Command} {parameters}");
				return;
			}

			HandleCore(sourceMessageId, channel, sender, parameters);
		}

		protected abstract void HandleCore(string? sourceMessageId, string channel, MessageSource sender, string? parameters);

		protected bool IsCommandEnabledForChannel(string channel, string command)
		{
			var channelTwitchUser = TwitchRepository.GetUser(channel[1..]);
			if (channelTwitchUser == null)
			{
				return true;
			}

			var settings = BotRepository.ListZsemlebotSettings(p => p.ChannelTwitchUserId == channelTwitchUser.TwitchUserId && p.TargetTwitchUserId == null && p.SettingName == command);
			if (settings.Count == 0)
			{
				return true;
			}

			var setting = settings[0];
			return setting.SettingValue == Constants.Settings_Enable;
		}

		protected (TwitchUser?, IReadOnlyList<HotaUser>) ListHotaUsers(string channel, string? parameters)
		{
			string targetName;
			if (string.IsNullOrWhiteSpace(parameters))
			{
				//get elo for current channel
				targetName = channel[1..];
			}
			else
			{
				targetName = parameters;

				//auto complete adds @ at the beginning of twitch usernames
				if (targetName.StartsWith('@'))
				{
					targetName = targetName[1..];
				}
			}

			var twitchUser = TwitchRepository.GetUser(targetName);
			var hotaUsers = new List<HotaUser>();
			if (twitchUser != null)
			{
				//get linked users
				var links = BotRepository.GetLinksForTwitchUser(twitchUser);
				hotaUsers.AddRange(links.LinkedHotaUsers);
			}

			var hotaUser = HotaRepository.GetUser(targetName);
			if (hotaUser != null && !hotaUsers.Any(hu => hu.HotaUserId == hotaUser.HotaUserId))
			{
				hotaUsers.Add(hotaUser);
			}

			return (twitchUser, hotaUsers);
		}
	}
}
