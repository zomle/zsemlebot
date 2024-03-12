using zsemlebot.core;
using zsemlebot.repository;
using zsemlebot.twitch;

namespace zsemlebot.services.Commands
{

	public class ChannelCommand : TwitchCommand
	{
		public override string Command { get { return Constants.Command_Channel; } }

		public ChannelCommand(TwitchService twitchService, HotaService hotaService)
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

			var tokens = parameters.Split(' ');
			if (tokens.Length != 2
				|| (tokens[0] != "add" && tokens[0] != "del")
				|| tokens[1][0] != '#')
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ChannelInvalidMessage());
				return;
			}

			var targetChannel = tokens[1];
			var targetUserName = targetChannel[1..];
			var targetUser = TwitchRepository.GetUser(targetUserName);

			if (targetUser == null)
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.TwitchUserNotFound(targetUserName));
				return;
			}

			if (tokens[0] == "add")
			{
				var hasJoinedTheChannel = BotRepository.HasJoinedChannel(targetUser.TwitchUserId);
				if (hasJoinedTheChannel)
				{
					TwitchService.TrySendJoinCommand(targetChannel);
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.JoiningChannel(targetChannel));
				}
				else
				{
					BotRepository.AddJoinedChannel(targetUser.TwitchUserId);

					TwitchService.TrySendJoinCommand(targetChannel);
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.JoiningChannel(targetChannel));
				}
			}
			else if (tokens[0] == "del")
			{
				var hasJoinedTheChannel = BotRepository.HasJoinedChannel(targetUser.TwitchUserId);
				if (hasJoinedTheChannel)
				{
					BotRepository.DeleteJoinedChannel(targetUser.TwitchUserId);

					TwitchService.TrySendPartCommand(targetChannel);
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.LeavingChannel(targetChannel));
				}
				else
				{
					TwitchService.TrySendPartCommand(targetChannel);
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.LeavingChannel(targetChannel));
				}
			}
			else
			{
				// should never happen
				return;
			}
		}
	}
}
