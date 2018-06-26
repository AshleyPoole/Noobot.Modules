using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Common.Logging;

using Newtonsoft.Json;

using Noobot.Core.Configuration;
using Noobot.Core.Plugins;
using Noobot.Modules.Cloudflare.CloudflareApi;

namespace Noobot.Modules.Cloudflare
{
	public class CloudflarePlugin : IPlugin
	{
		private readonly IConfigReader configReader;

		private readonly ILog log;

		private string apiUrl;

		private string authKey;

		private string authEmail;

		public CloudflarePlugin(IConfigReader configReader, ILog log)
		{
			this.configReader = configReader;
			this.log = log;
		}

		public void Start()
		{
			this.authEmail = this.configReader.GetConfigEntry<string>("cloudflare:authemail");
			this.authKey = this.configReader.GetConfigEntry<string>("cloudflare:authkey");
			this.apiUrl = this.configReader.GetConfigEntry<string>("cloudflare:apiurl");
		}

		public void Stop()
		{
		}

		internal CommandResult PurgeZone(string zoneName)
		{
			try
			{
				var zone = this.GetZoneByName(zoneName).Result;

				if (zone == null)
				{
					return new CommandResult { Success = false, Message = $"Failed to find zone with name `{zoneName}`" };
				}

				var deleteResponse = this.MakeDeleteApiRequest($"/zones/{zone.Id}/purge_cache", "{\"purge_everything\":true}").Result;

				if (!deleteResponse.Success)
				{
					return new CommandResult
					{
								Success = false,
								Message = $"Found zone with name `{zoneName}` but failed to purge cache"
							};
				}

				return new CommandResult { Success = true, Message = $"Successfully purged cache for zone `{zoneName}`" };
			}
			catch (Exception e)
			{
				this.log.Error(e);
				return new CommandResult { Success = false, Message = $"Something went wrong while processing purge request for zone `{zoneName}`" };
			}
		}

		internal CommandResult PurgeZoneCacheTag(string zoneName, string cacheTag)
		{
			try
			{
				var zone = this.GetZoneByName(zoneName).Result;

				if (zone == null)
				{
					return new CommandResult { Success = false, Message = $"Failed to find zone with name `{zoneName}`" };
				}

				var deleteResponse = this.MakeDeleteApiRequest($"/zones/{zone.Id}/purge_cache", $"{{\"tags\":[\"{cacheTag}\"]}}").Result;

				if (!deleteResponse.Success)
				{
					return new CommandResult
							{
								Success = false,
								Message = $"Found zone with name `{zoneName}` but failed to purge cache for tag `{cacheTag}`"
							};
				}

				return new CommandResult { Success = true, Message = $"Successfully purged cache tag `{cacheTag}` for zone `{zoneName}`" };
			}
			catch (Exception e)
			{
				this.log.Error(e);
				return new CommandResult { Success = false, Message = $"Something went wrong while processing purge request tag `{cacheTag}` for zone `{zoneName}`" };
			}
		}

		private async Task<Result> GetZoneByName(string zoneName)
		{
			var apiResponse = await this.MakeGetApiRequest($"/zones?name={zoneName}");

			if (apiResponse.Success && apiResponse.ResultInfo.Count == 1)
			{
				return apiResponse.Result.First();
			}

			this.log.Info($"{nameof(CloudflarePlugin)} failed to find zone for {zoneName}. Error from API: {string.Join(",", apiResponse.Errors)}");

			return null;
		}

		private async Task<CloudflareApiSingleResponse> MakeDeleteApiRequest(string queryString, string requestBody)
		{
			using (var handler = new HttpClientHandler())
			{
				using (var client = new HttpClient(handler))
				{
					client.DefaultRequestHeaders.Add("X-Auth-Email", this.authEmail);
					client.DefaultRequestHeaders.Add("X-Auth-Key", this.authKey);

					var requestMessage =
						new HttpRequestMessage(HttpMethod.Delete, new Uri(this.apiUrl + queryString))
						{
							Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
						};

					var request = await client.SendAsync(requestMessage);
					var jsonString = await request.Content.ReadAsStringAsync();
					return JsonConvert.DeserializeObject<CloudflareApiSingleResponse>(jsonString, Converter.Settings);
				}
			}
		}

		private async Task<CloudflareApiResponse> MakeGetApiRequest(string queryString)
		{
			using (var handler = new HttpClientHandler())
			{
				using (var client = new HttpClient(handler))
				{
					client.DefaultRequestHeaders.Add("X-Auth-Email", this.authEmail);
					client.DefaultRequestHeaders.Add("X-Auth-Key", this.authKey);

					var request = await client.GetAsync(this.apiUrl + queryString);
					var jsonString = await request.Content.ReadAsStringAsync();
					return JsonConvert.DeserializeObject<CloudflareApiResponse>(jsonString, Converter.Settings);
				}
			}
		}
	}
}
