using System;

using Common.Logging;

using NewRelic;

using Noobot.Core.Configuration;
using Noobot.Core.Plugins;

namespace Noobot.Modules.NewRelic
{
	public class NewRelicPlugin : IPlugin
	{
		private readonly IConfigReader configReader;

		private string apiKey;

		public NewRelicPlugin(IConfigReader configReader)
		{
			this.configReader = configReader;
		}

		public void Start()
		{
			this.apiKey = this.configReader.GetConfigEntry<string>("newrelic:apikey");
		}

		public void Stop()
		{
		}

		internal NewRelicRestClient GetNewRelicClient()
		{
			return new NewRelicRestClient(this.apiKey);
		}

		internal static bool ApplicationTargetedCommandWellFormatted(string message)
		{
			return message.Split(" ", StringSplitOptions.RemoveEmptyEntries).Length == 4;
		}

		internal int GetAccountIdFromApplicationTargeted(string message)
		{
			var accountId = message.Split(" ", StringSplitOptions.RemoveEmptyEntries)[3];
			return int.Parse(accountId);
		}
	}
}
