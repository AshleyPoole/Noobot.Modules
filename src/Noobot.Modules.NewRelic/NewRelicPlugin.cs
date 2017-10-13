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

		private readonly ILog log;

		private string apiKey;

		public NewRelicPlugin(IConfigReader configReader, ILog log)
		{
			this.configReader = configReader;
			this.log = log;
		}

		public void Start()
		{
			this.apiKey = this.configReader.GetConfigEntry<string>("newrelic:apikey");
		}

		public void Stop()
		{
		}

		public NewRelicRestClient GetNewRelicClient()
		{
			return new NewRelicRestClient(this.apiKey);
		}

		public static bool ApplicationTargetedCommandWellFormatted(string message)
		{
			return message.Split(" ", StringSplitOptions.RemoveEmptyEntries).Length == 4;
		}

		public int GetAccountIdFromApplicationTargeted(string message)
		{
			var accountId = message.Split(" ", StringSplitOptions.RemoveEmptyEntries)[3];
			return int.Parse(accountId);
		}
	}
}
