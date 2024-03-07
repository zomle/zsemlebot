using System;
using zsemlebot.core;
using zsemlebot.core.Domain;

namespace zsemlebot.twitch
{
	public class MessageSource
	{
		public TwitchUser TwitchUser { get; }
		public int TwitchUserId { get { return TwitchUser.TwitchUserId; } }
		public string DisplayName { get { return TwitchUser.DisplayName; } }

		public bool IsBroadcaster { get; set; }
		public bool IsModerator { get; set; }
		public bool IsVip { get; set; }
		
		public bool IsAdmin { get { return TwitchUserId == Config.Instance.Twitch.AdminUserId; } }
		public bool IsModOrBroadcaster { get { return IsBroadcaster || IsModerator; } }

		public MessageSource(TwitchUser twitchUser)
        {
			TwitchUser = twitchUser;
		}
    }
}
