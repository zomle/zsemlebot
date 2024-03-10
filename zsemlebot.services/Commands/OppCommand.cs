using System.Linq;
using System.Threading;
using zsemlebot.twitch;

namespace zsemlebot.services.Commands
{
	public class OppCommand : TwitchCommand
	{
		public override string Command { get { return Constants.Command_Opp; } }

		public OppCommand(TwitchService twitchService, HotaService hotaService) 
			: base(twitchService, hotaService)
		{
		}

		protected override void HandleCore(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			var (_, queriedHotaUsers) = ListHotaUsers(channel, null);

			var games = HotaService.FindGameForUsers(queriedHotaUsers);
			if (games.Count == 0)
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.GameNotFound(channel[1..]));
			}
			else
			{
				var game = games[0];

				new Thread(() =>
				{
					var opps = game.Game.JoinedPlayers
									.Where(jp => jp.HotaUserId != game.UserOfInterest.HotaUserId)
									.Select(jp => jp.HotaUser)
									.ToList();

					HotaService.RequestUserRepAndWait(opps);
					HotaService.RequestUserEloAndWait(opps);

					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.OppDescriptions(opps));
				}).Start();
			}
		}
	}
}
