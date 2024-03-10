using System.Linq;
using System.Threading;
using zsemlebot.twitch;

namespace zsemlebot.services.Commands
{
	public class RepCommand : TwitchCommand
	{
		public override string Command { get { return Constants.Command_Rep; } }

		public RepCommand(TwitchService twitchService, HotaService hotaService) 
			: base(twitchService, hotaService)
		{
		}

		protected override void HandleCore(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			var (twitchUser, hotaUsers) = ListHotaUsers(channel, parameters);
			if (hotaUsers.Count == 0)
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.UserNotFound(parameters ?? channel[1..]));
				return;
			}

			new Thread(() =>
			{
				var response = HotaService.RequestUserRepAndWait(hotaUsers);

				if (twitchUser != null)
				{
					var message = MessageTemplates.RepForTwitchUser(twitchUser, response.UpdatedUsers.Concat(response.NotUpdatedUsers));
					TwitchService.SendChatMessage(sourceMessageId, channel, message);
				}
				else
				{
					var message = MessageTemplates.RepForHotaUser(response.UpdatedUsers.Concat(response.NotUpdatedUsers).ToList());
					TwitchService.SendChatMessage(sourceMessageId, channel, message);
				}
			}).Start();
		}
	}
}
