using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

using Common.Logging;

using Newtonsoft.Json;

using Noobot.Core.Configuration;
using Noobot.Core.Plugins;
using Noobot.Modules.LoadBalancerDotOrg.Models;

namespace Noobot.Modules.LoadBalancerDotOrg
{
	public class LboPlugin : IPlugin
	{
		private readonly IConfigReader configReader;
		private readonly ILog log;

		public LboPlugin(IConfigReader configReader, ILog log)
		{
			this.configReader = configReader;
			this.log = log;
		}

		public void Start()
		{
		}

		public void Stop()
		{
		}

		internal static bool CommandWellFormatted(string message)
		{
			return message.Split(" ", StringSplitOptions.RemoveEmptyEntries).Length == 5;
		}

		internal LoadBalanacerAppliance GetAppliance(string applianceName)
		{
			try
			{
				var applianceConfig = this.configReader.GetConfigEntry<string>($"{Configuration.Prefix}:{applianceName}");
				var appliance = JsonConvert.DeserializeObject<LoadBalanacerAppliance>(applianceConfig);
				return appliance;
			}
			catch (Exception e)
			{
				this.log.Error($"Unable to find load balancer appliance with name:{applianceName}", e);
				return null;
			}
		}

		internal HttpResponseMessage MakeApiRequest(LoadBalanacerAppliance appliance, LoadBalancerRequest request)
		{
			using (var handler = new HttpClientHandler())
			{
				if (this.TrustAllCerts)
				{
					handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
				}

				using (var client = new HttpClient(handler))
				{
					client.DefaultRequestHeaders.Authorization = GetAuthHeader(appliance);
					return client.PostAsync(appliance.ApiUrl, GetRequestContent(appliance, request)).Result;
				}
			}
		}

		internal bool ApiRequestThrewException(HttpResponseMessage responseMessage, Exception exception)
		{
			return exception != null || responseMessage == null || !responseMessage.IsSuccessStatusCode;
		}

		internal string ParseApiResponse(HttpResponseMessage responseMessage)
		{
			var lbResponse = responseMessage.Content.ReadAsStringAsync().Result;
			return lbResponse.Replace("\n", string.Empty).Replace("LBCLI:", string.Empty);
		}

		private bool TrustAllCerts => this.configReader.GetConfigEntry<bool>($"{Configuration.Prefix}:trustAllCerts");

		private static AuthenticationHeaderValue GetAuthHeader(LoadBalanacerAppliance appliance)
		{
			var bytes = Encoding.ASCII.GetBytes($"{appliance.Username}:{appliance.Password}");
			return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));
		}

		private static StringContent GetRequestContent(LoadBalanacerAppliance applaince, LoadBalancerRequest request)
		{
			var apiRequest = new ApiRequest(applaince.ApiKey, request.Command, request.Vip, request.Rip);
			return new StringContent(JsonConvert.SerializeObject(apiRequest), Encoding.UTF8);
		}
	}
}
