using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using zsemlebot.core;
using zsemlebot.core.Domain;
using zsemlebot.twitch;

namespace zsemlebot.services
{
	public partial class TwitchService
	{
		private void HandleCommand(string? sourceMessageId, string channel, MessageSource sender, string command, string? parameters)
		{
			switch (command)
			{
				case Constants.Command_Channel:
					HandleChannelCommand(sourceMessageId, channel, sender, parameters);
					break;

				case Constants.Command_Elo:
					HandleEloCommand(sourceMessageId, channel, sender, parameters);
					break;

				case Constants.Command_Game:
					HandleGameCommand(sourceMessageId, channel, sender, parameters);
					break;

				case Constants.Command_Leave:
					HandleLeaveCommand(sourceMessageId, channel, sender, parameters);
					break;

				case Constants.Command_Link:
					HandleLinkCommand(sourceMessageId, channel, sender, parameters);
					break;

				case Constants.Command_LinkMe:
					HandleLinkMeCommand(sourceMessageId, channel, sender, parameters);
					break;

				case Constants.Command_Opp:
					HandleOppCommand(sourceMessageId, channel, sender, parameters);
					break;

				case Constants.Command_Rep:
					HandleRepCommand(sourceMessageId, channel, sender, parameters);
					break;
			}
		}

		private void HandleChannelCommand(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			if (parameters == null)
			{
				return;
			}

			if (!sender.IsAdmin)
			{
				return;
			}

			if (channel[1..] != Config.Instance.Twitch.AdminChannel)
			{
				return;
			}

			var tokens = parameters.Split(' ');
			if (tokens.Length != 2
				|| (tokens[0] != "add" && tokens[0] != "del")
				|| tokens[1][0] != '#')
			{
				SendChatMessage(sourceMessageId, channel, MessageTemplates.ChannelInvalidMessage());
				return;
			}

			var targetChannel = tokens[1];
			var targetUserName = targetChannel[1..];
			var targetUser = TwitchRepository.GetUser(targetUserName);

			if (targetUser == null)
			{
				SendChatMessage(sourceMessageId, channel, MessageTemplates.TwitchUserNotFound(targetUserName));
				return;
			}

			if (tokens[0] == "add")
			{
				var hasJoinedTheChannel = BotRepository.HasJoinedChannel(targetUser.TwitchUserId);
				if (hasJoinedTheChannel)
				{
					TrySendJoinCommand(targetChannel);
					SendChatMessage(sourceMessageId, channel, MessageTemplates.JoiningChannel(targetChannel));
				}
				else
				{
					BotRepository.AddJoinedChannel(targetUser.TwitchUserId);

					TrySendJoinCommand(targetChannel);
					SendChatMessage(sourceMessageId, channel, MessageTemplates.JoiningChannel(targetChannel));
				}
			}
			else if (tokens[0] == "del")
			{
				var hasJoinedTheChannel = BotRepository.HasJoinedChannel(targetUser.TwitchUserId);
				if (hasJoinedTheChannel)
				{
					BotRepository.DeleteJoinedChannel(targetUser.TwitchUserId);

					TrySendPartCommand(targetChannel);
					SendChatMessage(sourceMessageId, channel, MessageTemplates.LeavingChannel(targetChannel));
				}
				else
				{
					TrySendPartCommand(targetChannel);
					SendChatMessage(sourceMessageId, channel, MessageTemplates.LeavingChannel(targetChannel));
				}
			}
			else
			{
				// should never happen
				return;
			}
		}

		private void HandleLeaveCommand(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			if (parameters == null)
			{
				return;
			}

			if (!sender.IsBroadcaster && !sender.IsAdmin)
			{
				return;
			}

			if (!string.Equals(channel[1..], parameters, StringComparison.InvariantCultureIgnoreCase))
			{
				return;
			}

			var targetChannel = parameters;
			var targetUserName = targetChannel[1..];
			var targetUser = TwitchRepository.GetUser(targetUserName);
			if (targetUser == null)
			{
				return;
			}

			BotRepository.DeleteJoinedChannel(targetUser.TwitchUserId);

			SendChatMessage(sourceMessageId, channel, MessageTemplates.LeavingChannel(targetChannel));
			TrySendPartCommand(targetChannel);			
		}

		private void HandleOppCommand(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			var (_, queriedHotaUsers) = ListHotaUsers(channel, null);

			var games = HotaService.FindGameForUsers(queriedHotaUsers);
			if (games.Count == 0)
			{
				SendChatMessage(sourceMessageId, channel, MessageTemplates.GameNotFound(channel[1..]));
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

					SendChatMessage(sourceMessageId, channel, MessageTemplates.OppDescriptions(opps));
				}).Start();
			}
		}

		private void HandleGameCommand(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			if (string.IsNullOrEmpty(parameters))
			{
				HandleGameCommandQueryCurrentChannel(sourceMessageId, channel, sender, parameters);
			}
			else
			{
				var tokens = parameters.Split(new[] { ' ' }, 2);
				if (tokens[0] == "edit" && tokens.Length >= 1)
				{
					HandleGameCommandEdit(sourceMessageId, channel, sender, parameters);
				}
				else
				{
					HandleGameCommandQueryOtherUser(sourceMessageId, channel, sender, parameters);
				}
			}
		}

		private void HandleGameCommandQueryCurrentChannel(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			var queriedHotaUsers = ListHotaUsers(channel, null);

			HandleGameCommandForHotaUsers(sourceMessageId, channel, channel[1..], queriedHotaUsers.Item2);
		}

		private void HandleGameCommandQueryOtherUser(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
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

		private void HandleGameCommandEdit(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			if (string.IsNullOrEmpty(parameters))
			{
				return;
			}

			if (!IsUserAllowedToEditGame(sender))
			{
				return;
			}

			var tokens = parameters.Split(' ');

			var (twitchUser, queriedHotaUsers) = ListHotaUsers(channel, null);
			var foundGames = HotaService.FindGameForUsers(queriedHotaUsers);
			if (foundGames.Count == 0)
			{
				SendChatMessage(sourceMessageId, channel, MessageTemplates.GameNotFound(channel[1..]));
				return;
			}
			else if (foundGames.Count > 1)
			{
				SendChatMessage(sourceMessageId, channel, MessageTemplates.MultipleGamesFound(channel[1..]));
				return;
			}

			twitchUser ??= TwitchRepository.GetUser(channel[1..]);

			var game = foundGames[0].Game;
			var hotaUser = foundGames[0].UserOfInterest;
			var (startCollector, playerCollector) = SeparateArguments(twitchUser, hotaUser, game.JoinedPlayers, tokens[1..]);

			var templateName = GuessTemplate(startCollector.ToString());
			if (!string.IsNullOrWhiteSpace(templateName))
			{
				HotaService.UpdadateGameInfo(game, templateName);
				game.Template = templateName;
			}

			if (game.JoinedPlayers.Count == 2)
			{
				var player1 = game.JoinedPlayers[0];
				string? color1 = null;
				string? faction1 = null;
				int? tradeOutcome1 = null;
				if (playerCollector.TryGetValue(player1, out var player1Collector))
				{
					GuessProperties(player1Collector, ref tradeOutcome1, ref faction1, ref color1);
				}

				var player2 = game.JoinedPlayers[1];
				string? color2 = null;
				string? faction2 = null;
				int? tradeOutcome2 = null;
				if (playerCollector.TryGetValue(player2, out var player2Collector))
				{
					GuessProperties(player2Collector, ref tradeOutcome2, ref faction2, ref color2);
				}

				SetOtherTradeOutcome(ref tradeOutcome1, ref tradeOutcome2);
				SetOtherColor(ref color1, ref color2);

				if (color1 != null || faction1 != null || tradeOutcome1 != null)
				{
					HotaService.UpdatePlayerInfo(game, player1, color1, faction1, tradeOutcome1);
				}

				if (color2 != null || faction2 != null || tradeOutcome2 != null)
				{
					HotaService.UpdatePlayerInfo(game, player2, color2, faction2, tradeOutcome2);
				}
			}
			else
			{
				foreach (var playerTokens in playerCollector)
				{
					string? color = null;
					string? faction = null;
					int? tradeOutcome = null;
					GuessProperties(playerTokens.Value, ref tradeOutcome, ref faction, ref color);

					if (color != null || faction != null || tradeOutcome != null)
					{
						HotaService.UpdatePlayerInfo(game, playerTokens.Key, color, faction, tradeOutcome);
					}
				}
			}
		}

		private void GuessProperties(List<string> collector, ref int? tradeOutcome, ref string? faction, ref string? color)
		{
			foreach (var token in collector)
			{
				if (int.TryParse(token, out var trade))
				{
					tradeOutcome = trade;
					continue;
				}

				var tmp = GuessFaction(token);
				if (tmp != null)
				{
					faction = tmp;
				}

				tmp = GuessColor(token);
				if (tmp != null)
				{
					color = tmp;
				}
			}
		}

		private string? GuessColor(string input)
		{
			var lowerInput = input.ToLowerInvariant();

			switch (lowerInput)
			{
				case "red":
				case "blue":
				case "tan":
				case "green":
				case "orange":
				case "purple":
				case "teal":
				case "pink":
					return lowerInput;

				default:
					return null;
			}
		}

		private string? GuessFaction(string input)
		{
			var lowerInput = input.ToLowerInvariant();
			if (lowerInput == "castle")
			{
				return "Castle";
			}

			if (lowerInput == "cove")
			{
				return "Cove";
			}

			if (lowerInput.StartsWith("ramp"))
			{
				return "Rampart";
			}

			if (lowerInput == "tower")
			{
				return "Tower";
			}

			if (lowerInput.StartsWith("inf"))
			{
				return "Inferno";
			}

			if (lowerInput.StartsWith("necro"))
			{
				return "Necropolis";
			}

			if (lowerInput.StartsWith("dung"))
			{
				return "Dungeon";
			}

			if (lowerInput.StartsWith("strong"))
			{
				return "Stronghold";
			}

			if (lowerInput == "smorchold")
			{
				return "SMOrc hold";
			}

			if (lowerInput.StartsWith("fort"))
			{
				return "Fortress";
			}

			if (lowerInput.StartsWith("conf") || lowerInput == "flux")
			{
				return "Conflux";
			}

			if (lowerInput.StartsWith("fact"))
			{
				return "Factory";
			}

			return null;
		}

		private string? GuessTemplate(string input)
		{
			if (string.IsNullOrWhiteSpace(input))
			{
				return null;
			}

			var lowerInput = input.ToLowerInvariant();
			if (lowerInput == "6lm" || lowerInput == "6lm10" || lowerInput == "6lm10a")
			{
				return "6lm10a";
			}

			if (lowerInput == "8xm8")
			{
				return lowerInput;
			}

			if (lowerInput == "arcade")
			{
				return "Arcade";
			}

			if (lowerInput == "anarchy")
			{
				return "Anarchy";
			}

			if (lowerInput == "cod" || lowerInput == "clash")
			{
				return "Clash of Dragons";
			}
			else if (lowerInput.Contains("clash") && lowerInput.Contains("dragon"))
			{
				var dragonIndex = lowerInput.IndexOf("dragon");
				var firstSpaceIndex = lowerInput.IndexOf(' ', dragonIndex);
				var rest = "";
				if (firstSpaceIndex > dragonIndex)
				{
					rest = lowerInput.Substring(firstSpaceIndex);
				}
				return "Clash of Dragons" + rest;
			}

			if (lowerInput == "diamond")
			{
				return "Diamond";
			}

			if (lowerInput.Replace(" ", "") == "g+u")
			{
				return "g+u";
			}

			if (lowerInput == "h3dm" || lowerInput == "h3dm1")
			{
				return "h3dm1";
			}

			if (lowerInput == "jc" || lowerInput == "jebus")
			{
				return "Jebus Cross";
			}

			if (lowerInput == "jk")
			{
				return "Jebus King";
			}

			if (lowerInput.Contains("jcsmol") || lowerInput.Contains("jcsmall"))
			{
				return "mt_JCSmol";
			}

			if (lowerInput.Contains("nine") && lowerInput.Contains("day") && lowerInput.Contains("wonder"))
			{
				return "Nine-day Wonder(m200)";
			}

			if (lowerInput == "m200")
			{
				return "M200";
			}

			if (lowerInput.Contains("nosta") || lowerInput.Contains("nostalgia"))
			{
				if (lowerInput.Contains("mini"))
				{
					return "Mini Nostalgia";
				}
				else
				{
					return "Nostalgia";
				}
			}

			if (lowerInput == "spider")
			{
				return "Spider";
			}

			if (lowerInput == "ml")
			{
				return "Memory Lane";
			}

			return input;
		}

		private void SetOtherTradeOutcome(ref int? tradeOutcome1, ref int? tradeOutcome2)
		{
			if (tradeOutcome1 == null && tradeOutcome2 != null)
			{
				tradeOutcome1 = -tradeOutcome2;
			}
			else if (tradeOutcome1 != null && tradeOutcome2 == null)
			{
				tradeOutcome2 = -tradeOutcome1;
			}
		}

		private void SetOtherColor(ref string? color1, ref string? color2)
		{
			if (color1 == null && color2 != null)
			{
				if (string.Equals(color2, "red", StringComparison.InvariantCultureIgnoreCase))
				{
					color1 = "blue";
				}
				else if (string.Equals(color2, "blue", StringComparison.InvariantCultureIgnoreCase))
				{
					color1 = "red";
				}
			}
			else if (color1 != null && color2 == null)
			{
				if (string.Equals(color1, "red", StringComparison.InvariantCultureIgnoreCase))
				{
					color2 = "blue";
				}
				else if (string.Equals(color1, "blue", StringComparison.InvariantCultureIgnoreCase))
				{
					color2 = "red";
				}
			}
		}

		private bool IsUserAllowedToEditGame(MessageSource sender)
		{
			var allowed = sender.IsModOrBroadcaster || sender.IsAdmin;
			if (allowed)
			{
				return allowed;
			}

			return true;
		}

		private (string, Dictionary<HotaGamePlayer, List<string>>) SeparateArguments(TwitchUser mainTwitchUser, HotaUser mainHotaUser, IReadOnlyList<HotaGamePlayer> players, IEnumerable<string> tokens)
		{
			var templateNameCollector = new StringBuilder();
			var playerCollector = new Dictionary<HotaGamePlayer, List<string>>();

			var twitchName = mainTwitchUser.DisplayName;
			var mainPlayer = players.First(p => p.HotaUserId == mainHotaUser.HotaUserId);

			bool isInTemplate = true;
			HotaGamePlayer? currentPlayer = null;
			foreach (var token in tokens)
			{
				if (isInTemplate)
				{
					currentPlayer = GetMatchingPlayer(token, twitchName, mainPlayer, players);
					if (currentPlayer != null)
					{
						playerCollector.Add(currentPlayer, new List<string>());
						isInTemplate = false;
						continue;
					}

					if (templateNameCollector.Length > 0)
					{
						templateNameCollector.Append(' ');
					}

					templateNameCollector.Append(token);
				}
				else
				{
					var tmpPlayer = GetMatchingPlayer(token, twitchName, mainPlayer, players);
					if (tmpPlayer != null && tmpPlayer != currentPlayer && !playerCollector.ContainsKey(tmpPlayer))
					{
						currentPlayer = tmpPlayer;
						playerCollector.Add(currentPlayer, new List<string>());
						continue;
					}
					else
					{
						playerCollector[currentPlayer].Add(token);
					}
				}
			}

			return (templateNameCollector.ToString(), playerCollector);
		}

		private HotaGamePlayer? GetMatchingPlayer(string token, string twitchName, HotaGamePlayer mainPlayer, IReadOnlyCollection<HotaGamePlayer> players)
		{
			if (string.Equals(token, twitchName, StringComparison.InvariantCultureIgnoreCase)
						|| string.Equals(token, mainPlayer.PlayerName, StringComparison.InvariantCultureIgnoreCase)
						|| (token.Length > 3 && twitchName.StartsWith(token, StringComparison.InvariantCultureIgnoreCase)))
			{
				return mainPlayer;
			}
			else if (players.Count == 2 && string.Equals(token, "opp", StringComparison.InvariantCultureIgnoreCase))
			{
				return players.First(p => p != mainPlayer);
			}
			else if (token.Length == 3)
			{
				foreach (var player in players)
				{
					if (player.PlayerName.Equals(token, StringComparison.InvariantCultureIgnoreCase))
					{
						return player;
					}
				}
			}
			else if (token.Length > 3)
			{
				foreach (var player in players)
				{
					if (player.PlayerName.StartsWith(token, StringComparison.InvariantCultureIgnoreCase))
					{
						return player;
					}
				}
			}
			return null;
		}

		private void HandleLinkCommand(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			if (parameters == null)
			{
				return;
			}

			if (!sender.IsAdmin)
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

		private void HandleLinkMeCommand(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
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

		private void HandleEloCommand(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
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

		private void HandleRepCommand(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
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
