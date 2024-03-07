using System.Collections.Generic;
using zsemlebot.core.Domain;

namespace zsemlebot.services
{
	public class UserUpdateResponse
    {
        public List<HotaUser> UpdatedUsers { get; set; }
        public List<HotaUser> NotUpdatedUsers { get; set; }

        public UserUpdateResponse(IEnumerable<HotaUser> updatedUsers, IEnumerable<HotaUser> notUpdatedUsers)
        {
            UpdatedUsers = new List<HotaUser>(updatedUsers);
            NotUpdatedUsers = new List<HotaUser>(notUpdatedUsers);
        }
    }
}