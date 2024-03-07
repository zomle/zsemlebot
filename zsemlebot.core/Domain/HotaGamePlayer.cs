using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace zsemlebot.core.Domain
{
	public class HotaGamePlayer : IEqualityComparer<HotaGamePlayer>
	{
		public HotaUser HotaUser { get; set; }

		public uint HotaUserId { get { return HotaUser.HotaUserId; } }
		public string PlayerName { get { return HotaUser.DisplayName; } }

		public int StartElo { get; set; }
		public string? Faction { get; set; }
		public string? Color { get; set; }
		public int? Trade { get; set; }
		public int? PlayerEloChange { get; set; }


		public HotaGamePlayer(HotaUser hotaUser)
		{
			HotaUser = hotaUser;
		}

		public override bool Equals(object? obj)
		{
			return obj is HotaGamePlayer player &&
				   HotaUserId == player.HotaUserId;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(HotaUserId);
		}

		public bool Equals(HotaGamePlayer? x, HotaGamePlayer? y)
		{
			return x != null && y != null && x.Equals(y);
		}

		public int GetHashCode([DisallowNull] HotaGamePlayer obj)
		{
			return obj.GetHashCode();
		}
	}
}