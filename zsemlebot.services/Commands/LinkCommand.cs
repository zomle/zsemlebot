using System;
using System.Linq;
using zsemlebot.core;
using zsemlebot.repository;
using zsemlebot.twitch;

namespace zsemlebot.services.Commands
{
	public class LinkCommand : TwitchCommand
	{
		public override string Command { get { return Constants.Command_Link; } }

		public LinkCommand(TwitchService twitchService, HotaService hotaService) 
			: base(twitchService, hotaService)
		{
		}

		protected override void HandleCore(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			if (parameters == null)
			{
				return;
			}

			if (!sender.IsAdmin)
			{
				return;
			}

			if (channel[1..] != Config.Instance.Twitch.AdminChannel)
			{
				return;
			}

			var tokens = parameters.Split(new[] { ' ' }, 3);
			if (tokens.Length != 3
				|| (tokens[0] != "add" && tokens[0] != "del")
				|| !tokens[1].StartsWith(Constants.TwitchParameterPrefix)
				|| !tokens[2].StartsWith(Constants.HotaParameterPrefix))
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.UserLinkInvalidMessage());
				return;
			}

			string op = tokens[0];
			string twitchName = tokens[1][Constants.TwitchParameterPrefix.Length..];
			string hotaName = tokens[2][Constants.HotaParameterPrefix.Length..];

			var twitchUser = TwitchRepository.GetUser(twitchName);
			if (twitchUser == null)
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.TwitchUserNotFound(twitchName));
				return;
			}

			var hotaUser = HotaRepository.GetUser(hotaName);
			if (hotaUser == null)
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.HotaUserNotFound(hotaName));
				return;
			}

			var existingLinks = BotRepository.GetLinksForTwitchName(twitchName);
			if (existingLinks == null)
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.InvalidOperation("BotRepository.GetLinksForTwitchName() returned null in HandleLinkCommand()"));
				return;
			}

			if (op == "add")
			{
				if (existingLinks.LinkedHotaUsers.Any(hu => string.Equals(hu.DisplayName, hotaName, StringComparison.InvariantCultureIgnoreCase)))
				{
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.UserLinkAlreadyLinked(twitchName, hotaName));
					return;
				}

				BotRepository.AddTwitchHotaUserLink(twitchUser.TwitchUserId, hotaUser.HotaUserId);
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.UserLinkTwitchMessage(twitchName, hotaName));
			}
			else if (op == "del")
			{
				if (existingLinks.LinkedHotaUsers.Any(hu => string.Equals(hu.DisplayName, hotaName, StringComparison.InvariantCultureIgnoreCase)))
				{
					BotRepository.DelTwitchHotaUserLink(twitchUser.TwitchUserId, hotaUser.HotaUserId);
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.UserLinkDeleted(twitchName, hotaName));
				}
				else
				{
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.UserLinkDoesntExist(twitchName, hotaName));
				}
			}
		}
	}
}
