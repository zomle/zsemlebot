using System.Linq;
using zsemlebot.repository;
using zsemlebot.twitch;

namespace zsemlebot.services.Commands
{
	public class UnlinkMeCommand : TwitchCommand
	{
		public override string Command { get { return Constants.Command_UnLinkMe; } }

		public UnlinkMeCommand(TwitchService twitchService, HotaService hotaService)
			: base(twitchService, hotaService)
		{
		}

		protected override void HandleCore(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			if (parameters == null)
			{
				return;
			}

			var hotaName = parameters;

			var hotaUser = HotaRepository.GetUser(hotaName);
			if (hotaUser == null)
			{
				TwitchService.SendChatMessage(channel, MessageTemplates.HotaUserNotFound(hotaName));
				return;
			}

			var existingLinks = BotRepository.GetLinksForTwitchId(sender.TwitchUserId);
			if (existingLinks != null && existingLinks.LinkedHotaUsers.Any(h => h.HotaUserId == hotaUser.HotaUserId))
			{
				BotRepository.DelTwitchHotaUserLink(sender.TwitchUserId, hotaUser.HotaUserId);
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.UserLinkDeleted(sender.DisplayName, hotaName));
			}
			else
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.UserLinkDoesntExist(sender.DisplayName, hotaName));
			}
		}
	}
}
