using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using zsemlebot.core;

namespace zsemlebot.twitch
{
	public class HelixClient
	{
		private const string AppAccessTokenUrl = "https://id.twitch.tv/oauth2/token";

		private AccessTokenInfo AppAccessToken { get; set; }

		public HelixClient()
		{
		}

		public async Task RetrieveAppAccessToken()
		{
			using var client = new HttpClient();
			var content = new FormUrlEncodedContent(new[]
			{
				new KeyValuePair<string, string>("client_id", Config.Instance.Twitch.ClientId),
				new KeyValuePair<string, string>("client_secret",  Config.Instance.Twitch.ClientSecret),
				new KeyValuePair<string, string>("grant_type", "client_credentials")
			});

			var response = await client.PostAsync(AppAccessTokenUrl, content);
			AppAccessToken = await response.Content.ReadFromJsonAsync<AccessTokenInfo>();
		}

		public async Task<Dictionary<int, string>> GetUserInfo(IReadOnlyList<int> twitchUserIds)
		{
			await RetrieveAppAccessToken();

			using var client = new HttpClient();

			var baseUrl = "https://api.twitch.tv/helix/users";
			var url = baseUrl + "?" + string.Join("&", twitchUserIds.Select(id => $"id={id}"));

			var request = new HttpRequestMessage()
			{
				RequestUri = new Uri(url),
				Method = HttpMethod.Get,
			};

			request.Headers.Add("Authorization", $"Bearer {AppAccessToken.access_token}");
			request.Headers.Add("Client-Id", Config.Instance.Twitch.ClientId);
			
			var response = client.Send(request);
			var userInfos = await response.Content.ReadFromJsonAsync<UserInfoCollection>();

			var result = new Dictionary<int, string>();
			if (userInfos == null || userInfos.data == null)
			{
				return result;
			}

			foreach (var userInfo in userInfos.data)
			{
				result.Add(int.Parse(userInfo.id), userInfo.display_name);
			}

			return result;
		}
	}
}
