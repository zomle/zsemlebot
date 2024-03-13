using System;
using zsemlebot.repository;
using zsemlebot.twitch;

namespace zsemlebot.services.Commands
{
	public class LeaveCommand : TwitchCommand
	{
		public override string Command { get { return Constants.Command_Leave; } }

		public LeaveCommand(TwitchService twitchService, HotaService hotaService)
			: base(twitchService, hotaService)
		{
		}

		protected override void HandleCore(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			if (parameters == null)
			{
				return;
			}

			if (!sender.IsBroadcaster && !sender.IsAdmin)
			{
				return;
			}

			if (!string.Equals(channel[1..], parameters, StringComparison.InvariantCultureIgnoreCase))
			{
				return;
			}

			var targetUserName = parameters;
			var targetUser = TwitchRepository.GetUser(targetUserName);
			if (targetUser == null)
			{
				return;
			}

			var targetChannel = $"#{targetUserName}";
			BotRepository.DeleteJoinedChannel(targetUser.TwitchUserId);

			TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.LeavingChannel(targetChannel));
			TwitchService.TrySendPartCommand(targetChannel);
		}
	}
}
