using System;
using zsemlebot.repository;

namespace zsemlebot.services
{
    public class BotService : IDisposable
    {
		public void LoadUserData(string sourceDatabaseFilePath)
		{
			var oldRepository = new OldBotRepository(sourceDatabaseFilePath);
			
			var oldTwitchUsers = oldRepository.ListTwitchUsers();
			foreach (var user in oldTwitchUsers)
			{
				TwitchRepository.Instance.UpdateTwitchUserName(user.UserId, user.DisplayName);
			}

			var oldHotaUsers = oldRepository.ListHotaUsers();
			foreach(var user in oldHotaUsers)
			{
				var newUser = new core.Domain.HotaUser(user.UserId, user.UserName, user.UserElo, 0, null, user.LastUpdatedAtUtc);
				HotaRepository.Instance.UpdateHotaUser(newUser);
			}			
		}


        #region IDisposable implementation
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
					//
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
