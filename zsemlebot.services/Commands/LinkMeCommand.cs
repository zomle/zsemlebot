using System.Linq;
using zsemlebot.repository;
using zsemlebot.twitch;

namespace zsemlebot.services.Commands
{
	public class LinkMeCommand : TwitchCommand
	{
		public override string Command { get { return Constants.Command_LinkMe; } }

		public LinkMeCommand(TwitchService twitchService, HotaService hotaService) 
			: base(twitchService, hotaService)
		{
		}

		protected override void HandleCore(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			if (parameters == null)
			{
				return;
			}

			var requests = BotRepository.ListUserLinkRequests(sender.DisplayName);
			if (requests.Count == 0)
			{
				return;
			}

			var authCode = parameters;
			var request = requests.FirstOrDefault(r => r.AuthCode == authCode);
			if (request == null)
			{
				return;
			}

			var existingLinks = BotRepository.GetLinksForTwitchId(sender.TwitchUserId);
			if (existingLinks != null && existingLinks.LinkedHotaUsers.Any(h => h.HotaUserId == request.HotaUserId))
			{
				//link already exist
			}
			else
			{
				BotRepository.AddTwitchHotaUserLink(sender.TwitchUserId, request.HotaUserId);
			}
			BotRepository.DeleteUserLinkRequest(request.HotaUserId, request.TwitchUserName);

			var hotaUser = HotaRepository.GetUser(request.HotaUserId);
			if (hotaUser == null)
			{
				return;
			}

			if (sourceMessageId == null)
			{
				TwitchService.SendChatMessage(channel, MessageTemplates.UserLinkTwitchMessage(sender.DisplayName, hotaUser.DisplayName));
			}
			else
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.UserLinkTwitchMessage(sender.DisplayName, hotaUser.DisplayName));
			}
		}
	}
}
