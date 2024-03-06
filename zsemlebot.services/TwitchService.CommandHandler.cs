using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using zsemlebot.core;
using zsemlebot.core.Domain;

namespace zsemlebot.services
{
	public partial class TwitchService
	{
		private void HandleCommand(string? sourceMessageId, string channel, TwitchUser sender, string command, string? parameters)
		{
			switch (command)
			{
				case Constants.Command_Elo:
					HandleEloCommand(sourceMessageId, channel, sender, parameters);
					break;

				case Constants.Command_Game:
					HandleGameCommand(sourceMessageId, channel, sender, parameters);
					break;

				case Constants.Command_Link:
					HandleLinkCommand(sourceMessageId, channel, sender, parameters);
					break;

				case Constants.Command_LinkMe:
					HandleLinkMeCommand(sourceMessageId, channel, sender, parameters);
					break;

				case Constants.Command_Rep:
					HandleRepCommand(sourceMessageId, channel, sender, parameters);
					break;
			}
		}
		private void HandleGameCommand(string? sourceMessageId, string channel, TwitchUser sender, string? parameters)
		{
			if (string.IsNullOrEmpty(parameters))
			{
				HandleGameCommandQueryCurrentChannel(sourceMessageId, channel, sender, parameters);
			}
			else
			{
				var tokens = parameters.Split(new[] { ' ' }, 2);
				if (tokens[0] == "edit" && tokens.Length == 1)
				{
					HandleGameCommandEdit(sourceMessageId, channel, sender, parameters);
				}
				else
				{
					HandleGameCommandQueryOtherUser(sourceMessageId, channel, sender, parameters);
				}
			}
		}

		private void HandleGameCommandQueryCurrentChannel(string? sourceMessageId, string channel, TwitchUser sender, string? parameters)
		{
			var queriedHotaUsers = ListHotaUsers(channel, null);

			HandleGameCommandForHotaUsers(sourceMessageId, channel, channel[1..], queriedHotaUsers.Item2);
		}

		private void HandleGameCommandQueryOtherUser(string? sourceMessageId, string channel, TwitchUser sender, string? parameters)
		{
			if (parameters == null)
			{
				return;
			}

			var queriedUser = parameters;

			var queriedHotaUsers = new List<HotaUser>();
			var twitchUser = TwitchRepository.GetUser(queriedUser);
			if (twitchUser != null)
			{
				//supplied parameter is an existing twitch user;
				var links = BotRepository.GetLinksForTwitchUser(twitchUser);
				queriedHotaUsers.AddRange(links.LinkedHotaUsers);
			}

			var hotaUser = HotaRepository.GetUser(queriedUser);
			if (hotaUser != null && !queriedHotaUsers.Any(hu => hu.HotaUserId == hotaUser.HotaUserId))
			{
				queriedHotaUsers.Add(hotaUser);
			}

			HandleGameCommandForHotaUsers(sourceMessageId, channel, queriedUser, queriedHotaUsers);
		}

		private void HandleGameCommandForHotaUsers(string? sourceMessageId, string channel, string queriedUser, IEnumerable<HotaUser> queriedHotaUsers)
		{
			var games = HotaService.FindGameForUsers(queriedHotaUsers);
			if (games.Count == 0)
			{
				SendChatMessage(sourceMessageId, channel, MessageTemplates.GameNotFound(queriedUser));
			}
			else
			{
				var descriptions = new List<string>();
				foreach (var game in games)
				{
					var description = MessageTemplates.GameDescription(game);
					descriptions.Add(description);
				}

				SendChatMessage(sourceMessageId, channel, MessageTemplates.CurrentGames(descriptions));
			}
		}

		private void HandleGameCommandEdit(string? sourceMessageId, string channel, TwitchUser sender, string? parameters)
		{

		}

		private void HandleLinkCommand(string? sourceMessageId, string channel, TwitchUser sender, string? parameters)
		{
			if (parameters == null)
			{
				return;
			}

			if (sender.TwitchUserId != Config.Instance.Twitch.AdminUserId)
			{
				return;
			}

			if (channel[1..] != Config.Instance.Twitch.AdminChannel)
			{
				return;
			}

			var tokens = parameters.Split(new[] { ' ' }, 3);
			if (tokens.Length != 3
				|| (tokens[0] != "add" && tokens[0] != "del")
				|| !tokens[1].StartsWith(Constants.TwitchParameterPrefix)
				|| !tokens[2].StartsWith(Constants.HotaParameterPrefix))
			{
				SendChatMessage(sourceMessageId, channel, MessageTemplates.UserLinkInvalidMessage());
				return;
			}

			string op = tokens[0];
			string twitchName = tokens[1][Constants.TwitchParameterPrefix.Length..];
			string hotaName = tokens[2][Constants.HotaParameterPrefix.Length..];

			var twitchUser = TwitchRepository.GetUser(twitchName);
			if (twitchUser == null)
			{
				SendChatMessage(sourceMessageId, channel, MessageTemplates.TwitchUserNotFound(twitchName));
				return;
			}

			var hotaUser = HotaRepository.GetUser(hotaName);
			if (hotaUser == null)
			{
				SendChatMessage(sourceMessageId, channel, MessageTemplates.HotaUserNotFound(hotaName));
				return;
			}

			var existingLinks = BotRepository.GetLinksForTwitchName(twitchName);
			if (existingLinks == null)
			{
				SendChatMessage(sourceMessageId, channel, MessageTemplates.InvalidOperation("BotRepository.GetLinksForTwitchName() returned null in HandleLinkCommand()"));
				return;
			}

			if (op == "add")
			{
				if (existingLinks.LinkedHotaUsers.Any(hu => string.Equals(hu.DisplayName, hotaName, StringComparison.InvariantCultureIgnoreCase)))
				{
					SendChatMessage(sourceMessageId, channel, MessageTemplates.UserLinkAlreadyLinked(twitchName, hotaName));
					return;
				}

				BotRepository.AddTwitchHotaUserLink(twitchUser.TwitchUserId, hotaUser.HotaUserId);
				SendChatMessage(sourceMessageId, channel, MessageTemplates.UserLinkTwitchMessage(twitchName, hotaName));
			}
			else if (op == "del")
			{
				if (existingLinks.LinkedHotaUsers.Any(hu => string.Equals(hu.DisplayName, hotaName, StringComparison.InvariantCultureIgnoreCase)))
				{
					BotRepository.DelTwitchHotaUserLink(twitchUser.TwitchUserId, hotaUser.HotaUserId);
					SendChatMessage(sourceMessageId, channel, MessageTemplates.UserLinkDeleted(twitchName, hotaName));
				}
				else
				{
					SendChatMessage(sourceMessageId, channel, MessageTemplates.UserLinkDoesntExist(twitchName, hotaName));
				}
			}
		}

		private void HandleLinkMeCommand(string? sourceMessageId, string channel, TwitchUser sender, string? parameters)
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

			BotRepository.AddTwitchHotaUserLink(sender.TwitchUserId, request.HotaUserId);
			BotRepository.DeleteUserLinkRequest(request.HotaUserId, request.TwitchUserName);

			var hotaUser = HotaRepository.GetUser(request.HotaUserId);
			if (hotaUser == null)
			{
				return;
			}

			if (sourceMessageId == null)
			{
				SendChatMessage(channel, MessageTemplates.UserLinkTwitchMessage(sender.DisplayName, hotaUser.DisplayName));
			}
			else
			{
				SendChatMessage(sourceMessageId, channel, MessageTemplates.UserLinkTwitchMessage(sender.DisplayName, hotaUser.DisplayName));
			}
		}

		private void HandleEloCommand(string? sourceMessageId, string channel, TwitchUser sender, string? parameters)
		{
			var (twitchUser, hotaUsers) = ListHotaUsers(channel, parameters);
			if (hotaUsers.Count == 0)
			{
				SendChatMessage(sourceMessageId, channel, MessageTemplates.UserNotFound(parameters ?? channel[1..]));
				return;
			}

			new Thread(() =>
			{
				var response = HotaService.RequestUserEloAndWait(hotaUsers);

				if (twitchUser != null)
				{
					var message = MessageTemplates.EloForTwitchUser(twitchUser, response.UpdatedUsers.Concat(response.NotUpdatedUsers));
					SendChatMessage(sourceMessageId, channel, message);
				}
				else
				{
					var message = MessageTemplates.EloForHotaUser(response.UpdatedUsers.Concat(response.NotUpdatedUsers).ToList());
					SendChatMessage(sourceMessageId, channel, message);
				}
			}).Start();
		}

		private void HandleRepCommand(string? sourceMessageId, string channel, TwitchUser sender, string? parameters)
		{
			var (twitchUser, hotaUsers) = ListHotaUsers(channel, parameters);
			if (hotaUsers.Count == 0)
			{
				SendChatMessage(sourceMessageId, channel, MessageTemplates.UserNotFound(parameters ?? channel[1..]));
				return;
			}

			new Thread(() =>
			{
				var response = HotaService.RequestUserRepAndWait(hotaUsers);

				if (twitchUser != null)
				{
					var message = MessageTemplates.RepForTwitchUser(twitchUser, response.UpdatedUsers.Concat(response.NotUpdatedUsers));
					SendChatMessage(sourceMessageId, channel, message);
				}
				else
				{
					var message = MessageTemplates.RepForHotaUser(response.UpdatedUsers.Concat(response.NotUpdatedUsers).ToList());
					SendChatMessage(sourceMessageId, channel, message);
				}
			}).Start();
		}

		private (TwitchUser?, IReadOnlyList<HotaUser>) ListHotaUsers(string channel, string? parameters)
		{
			string targetName;
			if (string.IsNullOrWhiteSpace(parameters))
			{
				//get elo for current channel
				targetName = channel[1..];
			}
			else
			{
				targetName = parameters;

				//auto complete adds @ at the beginning of twitch usernames
				if (targetName.StartsWith('@'))
				{
					targetName = targetName[1..];
				}
			}

			var twitchUser = TwitchRepository.GetUser(targetName);
			IReadOnlyList<HotaUser> hotaUsers;
			if (twitchUser != null)
			{
				//get linked users
				var links = BotRepository.GetLinksForTwitchUser(twitchUser);
				hotaUsers = links.LinkedHotaUsers;
			}
			else
			{
				var hotaUser = HotaRepository.GetUser(targetName);
				if (hotaUser == null)
				{
					return (null, Array.Empty<HotaUser>());
				}
				hotaUsers = new HotaUser[] { hotaUser };
			}
			return (twitchUser, hotaUsers);
		}
	}
}
