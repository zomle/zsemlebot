using System;
using zsemlebot.core;
using zsemlebot.twitch;

namespace zsemlebot.services.Commands
{
	public class JoinMeCommand : TwitchCommand
	{
		public override string Command { get { return Constants.Command_JoinMe; } }

		public JoinMeCommand(TwitchService twitchService, HotaService hotaService)
			: base(twitchService, hotaService)
		{
		}

		protected override void HandleCore(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			if (parameters == null)
			{
				return;
			}

			if (channel[1..] != Config.Instance.Twitch.AdminChannel)
			{
				return;
			}

			if (!string.Equals(parameters, sender.DisplayName, StringComparison.InvariantCultureIgnoreCase))
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.JoinMeInvalidParameter(sender.DisplayName));
				return;
			}

			var targetChannel = $"#{sender.DisplayName}".ToLowerInvariant();
			var hasJoinedTheChannel = BotRepository.HasJoinedChannel(sender.TwitchUserId);
			if (hasJoinedTheChannel)
			{
				TwitchService.TrySendJoinCommand(targetChannel);
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.JoiningChannel(targetChannel));
			}
			else
			{
				BotRepository.AddJoinedChannel(sender.TwitchUserId);

				TwitchService.TrySendJoinCommand(targetChannel);
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.JoiningChannel(targetChannel));
			}
		}
	}
}
