using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using zsemlebot.core.Domain;
using zsemlebot.core.Log;
using zsemlebot.repository;
using zsemlebot.twitch;

namespace zsemlebot.services.Commands
{
	public class GameCommand : TwitchCommand
	{
		public override string Command { get { return Constants.Command_Game; } }

		public GameCommand(TwitchService twitchService, HotaService hotaService) 
			: base(twitchService, hotaService)
		{
		}

		protected override void HandleCore(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
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
			var (twitchUser, queriedHotaUsers) = ListHotaUsers(channel, parameters);

			HandleGameCommandForHotaUsers(sourceMessageId, channel, queriedUser, queriedHotaUsers);
		}

		private void HandleGameCommandForHotaUsers(string? sourceMessageId, string channel, string queriedUser, IEnumerable<HotaUser> queriedHotaUsers)
		{
			var games = HotaService.FindGameForUsers(queriedHotaUsers);
			if (games.Count == 0)
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.GameNotFound(queriedUser));
			}
			else if (games.Count == 1)
			{
				var gameInfo = games[0];
				bool isGameUnset = gameInfo.Game.Template == null && gameInfo.Game.JoinedPlayers.All(jp => jp.Color == null && jp.Faction == null);
				BotLogger.Instance.LogEvent(BotLogSource.Intrnl, $"{nameof(HandleGameCommandForHotaUsers)}(): Single game found. IsGameUnset: {isGameUnset}");

				if (isGameUnset)
				{
					new Thread(() =>
					{
						var response = HotaService.RequestGameHistoryAndWait(new[] { gameInfo.UserOfInterest }, true);
						if (response.UpdatedUsers.Count > 0)
						{
							var updatedUser = response.UpdatedUsers[0];
							var lastGame = updatedUser.GameHistory.Values.OrderByDescending(ghe => ghe.GameTimeInUtc).FirstOrDefault();
							//check if last game is the current game
							if (lastGame != null && lastGame.OutCome == 1)
							{
								foreach (var joinedPlayer in gameInfo.Game.JoinedPlayers)
								{
									HotaUserGameHistoryPlayer? historyPlayer = null;
									if (lastGame.Player1.UserId == joinedPlayer.HotaUserId)
									{
										historyPlayer = lastGame.Player1;
									}
									else if (lastGame.Player2.UserId == joinedPlayer.HotaUserId)
									{
										historyPlayer = lastGame.Player2;
									}
									else
									{
										historyPlayer = null;
									}

									if (historyPlayer != null)
									{
										var newColor = joinedPlayer.Color ?? historyPlayer.Color.ToString();
										var newFaction = joinedPlayer.Faction ?? historyPlayer.Town.ToString();
										HotaService.UpdatePlayerInfo(gameInfo.Game, joinedPlayer, newColor, newFaction, null);
									}
								}

								var mapNameResponse = HotaService.RequestMapNameAndWait(new[] { lastGame.MapId });
								if (mapNameResponse.MapNames.TryGetValue(lastGame.MapId, out var mapName)) 
								{
									var newTemplateName = gameInfo.Game.Template ?? mapName;
									HotaService.UpdateGameInfo(gameInfo.Game, newTemplateName);
								}
							}							
							
							var description = MessageTemplates.GameDescription(gameInfo);
							TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.CurrentGames(new[] { description }));
						}
						else
						{
							var description = MessageTemplates.GameDescription(gameInfo);
							TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.CurrentGames(new[] { description }));
						}
					}).Start();
				}
				else
				{
					var description = MessageTemplates.GameDescription(gameInfo);
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.CurrentGames(new[] { description }));
				}				
			}
			else
			{
				var descriptions = new List<string>();
				foreach (var game in games)
				{
					var description = MessageTemplates.GameDescription(game);
					descriptions.Add(description);
				}

				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.CurrentGames(descriptions));
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
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.GameNotFound(channel[1..]));
				return;
			}
			else if (foundGames.Count > 1)
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.MultipleGamesFound(channel[1..]));
				return;
			}

			twitchUser ??= TwitchRepository.GetUser(channel[1..]);

			var game = foundGames[0].Game;
			var hotaUser = foundGames[0].UserOfInterest;
			var (startCollector, playerCollector) = SeparateArguments(twitchUser, hotaUser, game.JoinedPlayers, tokens[1..]);

			var templateName = GuessTemplate(startCollector.ToString());
			if (!string.IsNullOrWhiteSpace(templateName))
			{
				HotaService.UpdateGameInfo(game, templateName);
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

	}
}
