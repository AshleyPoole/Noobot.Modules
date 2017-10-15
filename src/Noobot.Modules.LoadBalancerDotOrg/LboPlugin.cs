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
				Console.WriteLine(e);
				return null;
			}
		}

		internal bool TrustAllCerts => this.configReader.GetConfigEntry<bool>($"{Configuration.Prefix}:trustAllCerts");

		internal AuthenticationHeaderValue GetAuthHeader(LoadBalanacerAppliance appliance)
		{
			var bytes = Encoding.ASCII.GetBytes($"{appliance.Username}:{appliance.Password}");
			return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(bytes));
		}

		internal StringContent GetRequestContent(LoadBalanacerAppliance applaince, LoadBalancerRequest request)
		{
			var apiRequest = new ApiRequest(applaince.ApiKey, request.Command, request.Vip, request.Rip);
			return new StringContent(JsonConvert.SerializeObject(apiRequest), Encoding.UTF8);
		}
	}
}
