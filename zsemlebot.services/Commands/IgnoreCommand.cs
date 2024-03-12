using zsemlebot.core;
using zsemlebot.repository;
using zsemlebot.twitch;

namespace zsemlebot.services.Commands
{
	public class IgnoreCommand : TwitchCommand
	{
		public override string Command { get { return Constants.Command_Ignore; } }

		public IgnoreCommand(TwitchService twitchService, HotaService hotaService) 
			: base(twitchService, hotaService)
		{
		}

		protected override void HandleCore(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			if (!sender.IsAdmin || channel[1..] != Config.Instance.Twitch.AdminChannel)
			{
				return;
			}

			parameters ??= string.Empty;

			var tokens = parameters.Split(new[] { ' ' }, 2);
			if (tokens[0] == "add" && tokens.Length > 1)
			{
				var twitchUser = TwitchRepository.GetUser(tokens[1]);
				if (twitchUser == null)
				{
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.TwitchUserNotFound(tokens[1]));
					return;
				}

				TwitchRepository.AddUserToIgnoreList(twitchUser.TwitchUserId);
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.UserAddToIgnoreList(tokens[1]));
			}
			else if (tokens[0] == "del" && tokens.Length > 1) 
			{
				var twitchUser = TwitchRepository.GetUser(tokens[1]);
				if (twitchUser == null)
				{
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.TwitchUserNotFound(tokens[1]));
					return;
				}

				TwitchRepository.RemoveUserFromIgnoreList(twitchUser.TwitchUserId);
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.UserRemovedFromIgnoreList(tokens[1]));
			}
			else if (tokens[0] == "list")
			{
				var users = TwitchRepository.ListIgnoredUsers();
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.IgnoredUserList(users));
			}
			else
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.IgnoreInvalidMessage());
			}
		}
	}
}
