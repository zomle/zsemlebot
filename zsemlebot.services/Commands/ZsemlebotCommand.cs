using System;
using System.Linq;
using System.Text.RegularExpressions;
using zsemlebot.core.Domain;
using zsemlebot.repository.Models;
using zsemlebot.services.Log;
using zsemlebot.twitch;

namespace zsemlebot.services.Commands
{
	public class ZsemlebotCommand : TwitchCommand
	{
		public override string Command { get { return Constants.Command_Zsemlebot; } }

		public ZsemlebotCommand(TwitchService twitchService, HotaService hotaService)
			: base(twitchService, hotaService)
		{
		}

		protected override void HandleCore(string? sourceMessageId, string channel, MessageSource sender, string? parameters)
		{
			if (string.IsNullOrEmpty(parameters))
			{
				return;
			}

			var tokens = parameters.Split(' ', 2);
			if (tokens[0] == "enable")
			{
				/// Usage: !zsemlebot <enable|disable> <command>
				if (!sender.IsBroadcaster || !sender.IsAdmin)
				{
					return;
				}

				if (tokens.Length < 2)
				{
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotInvalidMessage());
					return;
				}

				HandleEnableDisableCommand(sourceMessageId, channel, sender, tokens[1], Constants.Settings_Enable);
			}
			else if (tokens[0] == "disable")
			{
				/// Usage: !zsemlebot <enable|disable> <command>
				if (!sender.IsBroadcaster || !sender.IsAdmin)
				{
					return;
				}

				if (tokens.Length < 2)
				{
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotInvalidMessage());
					return;
				}

				HandleEnableDisableCommand(sourceMessageId, channel, sender, tokens[1], Constants.Settings_Disable);
			}
			else if (tokens[0] == "set")
			{
				/// Usage: !zsemlebot set <option> <newvalue>
				if (!sender.IsBroadcaster || !sender.IsAdmin)
				{
					return;
				}

				if (tokens.Length < 2)
				{
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotInvalidMessage());
					return;
				}

				var args = tokens[1].Split(' ', 2);
				if (args.Length < 2)
				{
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotInvalidMessage());
					return;
				}

				var option = args[0];
				var newvalue = args[1];
				HandleSetUnsetValue(sourceMessageId, channel, sender, option, newvalue);
			}
			else if (tokens[0] == "unset")
			{
				/// Usage: !zsemlebot unset <option>
				if (!sender.IsBroadcaster || !sender.IsAdmin)
				{
					return;
				}

				if (tokens.Length < 2)
				{
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotInvalidMessage());
					return;
				}

				var option = tokens[1];
				HandleSetUnsetValue(sourceMessageId, channel, sender, option, null);
			}
			else if (tokens[0] == "setfor")
			{
				/// Usage for admin: !zsemlebot setfor <#targetchannel> <targetuser> <option> <newvalue>
				if (!sender.IsAdmin)
				{
					return;
				}

				if (tokens.Length < 2)
				{
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotInvalidMessage());
					return;
				}

				var args = tokens[1].Split(' ', 4);
				if (args.Length < 4)
				{
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotInvalidMessage());
					return;
				}

				var targetChannelName = args[0];
				var targetUserName = args[1];
				var providedOption = args[2];
				var newValue = args[3];

				HanelSetUnsetForValue(sourceMessageId, channel, sender, targetChannelName, targetUserName, providedOption, newValue);
			}
			else if (tokens[0] == "unsetfor")
			{
				/// Usage for admin: !zsemlebot unsetfor <targetchannel> <targetuser> <option>
				if (!sender.IsAdmin)
				{
					return;
				}

				if (tokens.Length < 2)
				{
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotInvalidMessage());
					return;
				}

				var args = tokens[1].Split(' ', 3);
				if (args.Length < 3)
				{
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotInvalidMessage());
					return;
				}

				var targetChannelName = args[0];
				var targetUserName = args[1];
				var providedOption = args[2];

				HanelSetUnsetForValue(sourceMessageId, channel, sender, targetChannelName, targetUserName, providedOption, null);
			}
			else if (tokens[0] == "get")
			{
				/// Usage for admin: !zsemlebot get <targetchannel> <targetuser> <option>
				if (!sender.IsAdmin)
				{
					return;
				}

				if (tokens.Length < 2)
				{
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotInvalidMessage());
					return;
				}

				var args = tokens[1].Split(' ', 3);
				if (args.Length < 3)
				{
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotInvalidMessage());
					return;
				}

				var targetChannelName = args[0];
				var targetUserName = args[1];
				var providedSettingName = args[2];

				HandleGetCommand(sourceMessageId, channel, sender, targetChannelName, targetUserName, providedSettingName);
			}
			else
			{
				if (!sender.IsBroadcaster || !sender.IsAdmin)
				{
					return;
				}

				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotInvalidMessage());
				return;
			}
		}

		private void HandleGetCommand(string? sourceMessageId, string channel, MessageSource sender, string targetChannelName, string targetUserName, string providedSettingName)
		{
			TwitchUser? targetChannelUser;
			if (targetChannelName == "global")
			{
				targetChannelUser = null;
			}
			else
			{
				if (!targetChannelName.StartsWith('#'))
				{
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotInvalidSetForMessage());
					return;
				}

				targetChannelUser = TwitchRepository.GetUser(targetChannelName[1..]);
				if (targetChannelUser == null)
				{
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.TwitchUserNotFound(targetChannelName[1..]));
					return;
				}
			}

			var targetUser = TwitchRepository.GetUser(targetUserName);
			if (targetUser == null)
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.TwitchUserNotFound(targetUserName));
				return;
			}

			var allSettingNames = new[]
			{
					Constants.Settings_TimeZone, Constants.Settings_CustomElo
				};

			if (!allSettingNames.Contains(providedSettingName))
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotInvalidOptionMessage(providedSettingName, allSettingNames));
				return;
			}

			Predicate<ZsemlebotSetting> predicate = p => p.SettingName == providedSettingName &&
														p.TargetTwitchUserId == targetUser.TwitchUserId &&
														(targetChannelUser == null
															|| p.ChannelTwitchUserId == targetChannelUser.TwitchUserId
															|| p.ChannelTwitchUserId == null);

			var settings = BotRepository.ListZsemlebotSettings(predicate);
			if (settings.Count == 0)
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotNoSettingsFound());
			}
			else
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotGetSetting(settings));
			}
		}

		private void HandleEnableDisableCommand(string? sourceMessageId, string channel, MessageSource sender, string providedCommand, string enableDisable)
		{
			var allCommands = new[]
			{
					Constants.Command_Elo, Constants.Command_Game, Constants.Command_Opp,
					Constants.Command_Rep,Constants.Command_Streak,Constants.Command_Today
			};

			if (!allCommands.Contains(providedCommand))
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotInvalidCommandMessage(providedCommand, allCommands));
				return;
			}

			BotRepository.UpdateZsemlebotSetting(null, sender.TwitchUserId, providedCommand, enableDisable);

			if (enableDisable == Constants.Settings_Enable)
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotCommandEnabled(providedCommand));
			}
			else if (enableDisable == Constants.Settings_Disable)
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotCommandDisabled(providedCommand));
			}
			else
			{
				BotLogger.Instance.LogEvent(BotLogSource.Intrnl, $"{nameof(ZsemlebotCommand)}.{nameof(HandleEnableDisableCommand)}() received an unhandled {nameof(enableDisable)} parameter: '{enableDisable}'");
			}
		}

		private void HandleSetUnsetValue(string? sourceMessageId, string channel, MessageSource sender, string providedOption, string? newValue)
		{
			var allOptions = new[]
			{
				Constants.Settings_TimeZone
			};

			if (!allOptions.Contains(providedOption))
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotInvalidOptionMessage(providedOption, allOptions));
				return;
			}

			if (newValue == null)
			{
				BotRepository.DeleteZsemlebotSetting(null, sender.TwitchUserId, providedOption);
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotSettingRemoved(providedOption));
				return;
			}

			if (providedOption == Constants.Settings_TimeZone)
			{
				var timeZoneInfo = GetTimeZoneText(newValue);
				if (timeZoneInfo == null)
				{
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotInvalidTimeZone(newValue));
					return;
				}
				newValue = timeZoneInfo;
			}

			BotRepository.UpdateZsemlebotSetting(null, sender.TwitchUserId, providedOption, newValue);
			TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotSettingUpdated(providedOption));
		}

		private void HanelSetUnsetForValue(string? sourceMessageId, string channel, MessageSource sender, string targetChannelName, string targetUserName, string providedOption, string? newValue)
		{
			TwitchUser? targetChannelUser;
			if (targetChannelName == "global")
			{
				targetChannelUser = null;
			}
			else
			{
				if (!targetChannelName.StartsWith('#'))
				{
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotInvalidSetForMessage());
					return;
				}

				targetChannelUser = TwitchRepository.GetUser(targetChannelName[1..]);
				if (targetChannelUser == null)
				{
					TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.TwitchUserNotFound(targetChannelName[1..]));
					return;
				}
			}

			var targetUser = TwitchRepository.GetUser(targetUserName);
			if (targetUser == null)
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.TwitchUserNotFound(targetUserName));
				return;
			}

			var allOptions = new[]
			{
				Constants.Settings_TimeZone, Constants.Settings_CustomElo
			};

			if (!allOptions.Contains(providedOption))
			{
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotInvalidOptionMessage(providedOption, allOptions));
				return;
			}

			if (newValue == null)
			{
				BotRepository.DeleteZsemlebotSetting(targetUser.TwitchUserId, targetChannelUser?.TwitchUserId, providedOption);
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotSettingRemoved(providedOption));
			}
			else
			{
				BotRepository.UpdateZsemlebotSetting(targetUser.TwitchUserId, targetChannelUser?.TwitchUserId, providedOption, newValue);
				TwitchService.SendChatMessage(sourceMessageId, channel, MessageTemplates.ZsemlebotSettingUpdated(providedOption));
			}
		}

		private static readonly Regex timeZoneRegex = new Regex("^utc\\s?(?<plusminus>[+-])\\s?(?<hours>\\d{1,2})(?::(?<mins>\\d{1,2}))?$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private string? GetTimeZoneText(string parameter)
		{
			if (string.Equals("utc", parameter, StringComparison.CurrentCultureIgnoreCase))
			{
				return "+00:00";
			}

			var match = timeZoneRegex.Match(parameter);
			if (!match.Success)
			{
				return null;
			}

			var plusMinusGroup = match.Groups["plusminus"];
			var hoursGroup = match.Groups["hours"];
			var minsGroup = match.Groups["mins"];

			var plusMinus = plusMinusGroup.Value;
			var hours = int.Parse(hoursGroup.Value);
			var mins = minsGroup.Success ? int.Parse(minsGroup.Value) : 0;

			return $"{plusMinus}{hours:00}:{mins:00}";
		}
	}
}
