using zsemlebot.core;
using zsemlebot.twitch;

namespace zsemlebot.services.Commands
{
	public class StatusCommand : ZsemlebotCommand
	{
		public override string Command { get { return Constants.Command_Status; } }

		public StatusCommand(TwitchService twitchService, HotaService hotaService)
			: base(twitchService, hotaService)
		{
		}

		protected override void HandleCore(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			if (!sender.IsAdmin || channel[1..] != Config.Instance.Twitch.AdminChannel)
			{
				return;
			}

			var message = MessageTemplates.StatusMessage(TwitchService.CurrentStatus, TwitchService.LastMessageReceivedAt, HotaService.CurrentStatus, HotaService.LastMessageReceivedAt);
			TwitchService.SendChatMessage(sourceMessageId, channel, message);
		}
	}
}
