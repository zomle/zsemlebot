using System;

namespace zsemlebot.core.Domain
{
    public class FakeHotaUser : HotaUser
    {
        public FakeHotaUser(uint hotaUserId)
            : base(hotaUserId, $"user not found ({hotaUserId})", -1, -1, null, DateTime.MinValue, null, false)
        {
        }
    }
}