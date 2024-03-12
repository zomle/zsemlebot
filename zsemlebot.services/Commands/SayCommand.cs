using System;
using zsemlebot.core;
using zsemlebot.twitch;

namespace zsemlebot.services.Commands
{
	public class SayCommand : ZsemlebotCommand
	{
		public override string Command { get { return Constants.Command_Say; } }

		public SayCommand(TwitchService twitchService, HotaService hotaService)
			: base(twitchService, hotaService)
		{
		}

		protected override void HandleCore(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			if (!sender.IsAdmin || channel[1..] != Config.Instance.Twitch.AdminChannel)
			{
				return;
			}

			if (string.IsNullOrWhiteSpace(parameters))
			{
				return;
			}

			var tokens = parameters.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
			if (tokens.Length != 2 || tokens[0][0] != '#')
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.SayInvalidFormat());
				return;
			}

			var targetChannel = tokens[0];
			var message = tokens[1];

			TwitchService.SendChatMessage(targetChannel, message + " beep-boop MrDestructoid");
		}
	}
}
