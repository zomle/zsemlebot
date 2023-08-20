using System;
using zsemlebot.core.Domain;
using zsemlebot.repository;

namespace zsemlebot.services
{
    internal class ServiceHelper
    {
        private TwitchRepository TwitchRepository { get { return TwitchRepository.Instance; } }
        private HotaRepository HotaRepository { get { return HotaRepository.Instance; } }
        private BotRepository BotRepository { get { return BotRepository.Instance; } }

        public ServiceHelper()
        {
        }
        
        public UserProfile GetProfileForTwitchName(string name)
        {
            throw new NotImplementedException();
        }

        public UserProfile GetProfileForHotaName(string name)
        {
            throw new NotImplementedException();
        }

        public UserProfile GetProfileForHotaUserId(int userId)
        {
            throw new NotImplementedException();
        }
    }
}
