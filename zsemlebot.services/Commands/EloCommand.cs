using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using zsemlebot.core.Domain;
using zsemlebot.twitch;

namespace zsemlebot.services.Commands
{
	public class EloCommand : TwitchCommand
	{
		public override string Command { get { return Constants.Command_Elo; } }

		public EloCommand(TwitchService twitchService, HotaService hotaService) 
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
				var response = HotaService.RequestUserEloAndWait(hotaUsers);

				if (twitchUser != null)
				{
					var settings = BotRepository.ListZsemlebotSettings(s => s.TargetTwitchUserId == twitchUser.TwitchUserId && s.SettingName == Constants.Settings_CustomElo);

					var channelTwitchUser = TwitchRepository.GetUser(channel[1..]);
					var setting = settings.FirstOrDefault(s => s.ChannelTwitchUserId == channelTwitchUser?.TwitchUserId);
					if (setting == null)
					{
						setting = settings.FirstOrDefault(s => s.ChannelTwitchUserId == null);
					}

					string eloMessage;
					if (setting != null)
					{
						eloMessage = FormatCustomEloMessage(twitchUser, setting.SettingValue, response.UpdatedUsers.Concat(response.NotUpdatedUsers));
					}
					else
					{
						eloMessage = MessageTemplates.EloForTwitchUser(twitchUser, response.UpdatedUsers.Concat(response.NotUpdatedUsers));
					}

					TwitchService.SendChatMessage(sourceMessageId, channel, eloMessage);
				}
				else
				{
					var message = MessageTemplates.EloForHotaUser(response.UpdatedUsers.Concat(response.NotUpdatedUsers).ToList());
					TwitchService.SendChatMessage(sourceMessageId, channel, message);
				}
			}).Start();
		}

		private string FormatCustomEloMessage(TwitchUser twitchUser, string? formatString, IEnumerable<HotaUser> hotaUsers)
		{
			if (formatString == null)
			{
				return MessageTemplates.EloForTwitchUser(twitchUser, hotaUsers);
			}

			string result = formatString;

			int maxElo = hotaUsers.Max(hu => hu.Elo);
			result = result.Replace(Constants.Settings_CustomElo_MaxEloOption, maxElo.ToString());

			int minElo = hotaUsers.Min(hu => hu.Elo);
			result = result.Replace(Constants.Settings_CustomElo_MinEloOption, minElo.ToString());

			var userElos = hotaUsers
					.OrderBy(hu => hu.DisplayName)
					.Select(hu => $"{hu.DisplayName}({hu.Elo})");
			result = result.Replace(Constants.Settings_CustomElo_AllElosOption, string.Join(", ", userElos));

			return result;
		}
	}
}
