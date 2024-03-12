using System.Collections.Generic;
using System.Linq;
using System.Threading;
using zsemlebot.core.Domain;
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
					var settings = BotRepository.ListZsemlebotSettings(s => s.TargetTwitchUserId == twitchUser.TwitchUserId && s.SettingName == Constants.Settings_CustomRep);

					var channelTwitchUser = TwitchRepository.GetUser(channel[1..]);
					var setting = settings.FirstOrDefault(s => s.ChannelTwitchUserId == channelTwitchUser?.TwitchUserId);
					if (setting == null)
					{
						setting = settings.FirstOrDefault(s => s.ChannelTwitchUserId == null);
					}

					string repMessage;
					if (setting != null)
					{
						repMessage = FormatCustomRepMessage(twitchUser, setting.SettingValue, response.UpdatedUsers.Concat(response.NotUpdatedUsers));
					}
					else
					{
						repMessage = MessageTemplates.RepForTwitchUser(twitchUser, response.UpdatedUsers.Concat(response.NotUpdatedUsers));
					}


					TwitchService.SendChatMessage(sourceMessageId, channel, repMessage);
				}
				else
				{
					var message = MessageTemplates.RepForHotaUser(response.UpdatedUsers.Concat(response.NotUpdatedUsers).ToList());
					TwitchService.SendChatMessage(sourceMessageId, channel, message);
				}
			}).Start();
		}

		private string FormatCustomRepMessage(TwitchUser twitchUser, string? formatString, IEnumerable<HotaUser> hotaUsers)
		{
			if (formatString == null)
			{
				return MessageTemplates.RepForTwitchUser(twitchUser, hotaUsers);
			}

			string result = formatString;

			int maxRep = hotaUsers.Max(hu => hu.Rep);
			result = result.Replace(Constants.Settings_CustomRep_MaxRepOption, maxRep.ToString());

			int minRep = hotaUsers.Min(hu => hu.Rep);
			result = result.Replace(Constants.Settings_CustomRep_MinRepOption, minRep.ToString());

			var userReps = hotaUsers
					.OrderBy(hu => hu.DisplayName)
					.Select(hu => $"{hu.DisplayName}({hu.Rep})");
			result = result.Replace(Constants.Settings_CustomRep_AllRepsOption, string.Join(", ", userReps));

			return result;
		}
	}
}
