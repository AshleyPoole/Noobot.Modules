using System;
using System.Linq;

using Noobot.Core.Configuration;
using Noobot.Core.Plugins;

using SlackConnector;
using SlackConnector.Models;

namespace Noobot.Modules.IncidentManagement
{
	public class IncidentManagementPlugin : IPlugin
	{
		private readonly IConfigReader configReader;

		public string MainIncidentChannel;

		private ISlackConnection SlackConnection { get; set; }

		public IncidentManagementPlugin(IConfigReader configReader)
		{
			this.configReader = configReader;
		}

		public void Start()
		{
			this.MainIncidentChannel = this.configReader.GetConfigEntry<string>("incident:mainchannel");

			var connector = new SlackConnector.SlackConnector();
			this.SlackConnection = connector.Connect(this.configReader.SlackApiKey).Result;
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

		internal void SendNewIncidentCreatedMessage(string incidentText, string reportedByUser, string incidentChannel)
		{
			var messageText = $"*NEW INCIDENT DECLARED*\n"
				+ $"Timestamp: { DateTime.UtcNow } GMT\n"
				+ $"Reported By: @{reportedByUser }\n"
				+ $"Channel: { this.GetUserFriendlyChannelName(incidentChannel) }\n"
				+ $"Description: { incidentText }\n";
			this.SendIncidentChannelMessage(messageText);
		}

		private string GetUserFriendlyChannelName(string channel)
		{
			return this.SlackConnection.GetChannels().Result.FirstOrDefault(x => x.Id == channel)?.Name;
		}

		private void SendIncidentChannelMessage(string messageText)
		{
			var chatHub = new SlackChatHub { Id = this.MainIncidentChannel };
			var message = new BotMessage { ChatHub = chatHub, Text = messageText };

			this.SlackConnection.Say(message);
		}
	}
}
