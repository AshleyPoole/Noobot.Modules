using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Noobot.Core.Configuration;
using Noobot.Core.Plugins;
using SlackAPI;
using SlackConnector;

namespace Noobot.Modules.NewRelic
{
	public class IncidentManagementPlugin : IPlugin
	{
		private readonly IConfigReader configReader;

		public string mainIncidentChannel;

		public string incidentChannelPrefix;

		private string apiKey;

		public IncidentManagementPlugin(IConfigReader configReader)
		{
			this.configReader = configReader;
		}

		public void Start()
		{
			this.apiKey = this.configReader.SlackApiKey;
			this.mainIncidentChannel = this.configReader.GetConfigEntry<string>("incident:mainchannel");
			this.incidentChannelPrefix = this.configReader.GetConfigEntry<string>("incident:channelprefix");
		}

		public void Stop()
		{
		}

		internal bool CommandWellFormatted(string message)
		{
			return message.Split(" ", StringSplitOptions.RemoveEmptyEntries).Length >= 3;
		}

		internal string GetIncidentText(string commandPrefix, string message)
		{
			return message.Replace(commandPrefix, string.Empty).Trim();
		}

		internal ISlackConnection GetSlackClient()
		{
			var connector = new SlackConnector.SlackConnector();
			return connector.Connect(apiKey).Result;
		}

		internal string GetNewChannelName(string incidentName)
		{
			var textInfo = new CultureInfo("en-US", false).TextInfo;
			var cleanedIncidentName = textInfo.ToTitleCase(incidentName).Replace(" ", string.Empty);
			var date = DateTime.UtcNow.ToString("yyMMdd");
			var channelName = $"{this.incidentChannelPrefix}-{date}-{cleanedIncidentName}";

			return new string(channelName.Take(21).ToArray());
		}

		internal bool ChannelExists(ISlackConnection slackConnection, string channelName)
		{
			return true;
		}
	}
}
