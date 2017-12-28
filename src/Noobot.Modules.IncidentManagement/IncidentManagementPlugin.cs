using System;
using System.Collections.Generic;
using System.Linq;

using Noobot.Core.Configuration;
using Noobot.Core.Plugins;
using Noobot.Modules.IncidentManagement.Models;

using SlackConnector;
using SlackConnector.Models;

namespace Noobot.Modules.IncidentManagement
{
	public class IncidentManagementPlugin : IPlugin
	{
		private readonly IConfigReader configReader;

		public string MainIncidentChannel;

		private readonly IncidentManagementStorageClient storageClient;

		private ISlackConnection SlackConnection { get; set; }

		public IncidentManagementPlugin(IConfigReader configReader)
		{
			this.configReader = configReader;
			this.storageClient = new IncidentManagementStorageClient(configReader);
		}

		public void Start()
		{
			this.MainIncidentChannel = this.configReader.GetConfigEntry<string>("incident:mainChannel");

			var connector = new SlackConnector.SlackConnector();
			this.SlackConnection = connector.Connect(this.configReader.SlackApiKey).Result;
		}

		public void Stop()
		{
		}

		internal bool NewIncidentCommandWellFormatted(string message)
		{
			return message.Split(" ", StringSplitOptions.RemoveEmptyEntries).Length >= 3;
		}

		internal string GetIncidentText(string commandPrefix, string message)
		{
			return message.Replace(commandPrefix, string.Empty).Trim();
		}
		internal string GetUserFriendlyChannelName(string channel)
		{
			return this.SlackConnection.GetChannels().Result.FirstOrDefault(x => x.Id == channel)?.Name;
		}

		internal Incident DeclareNewIncident(string incidentText, string reportedByUser, string incidentChannelName)
		{
			var incident = new Incident(incidentText, incidentChannelName, reportedByUser);
			incident.SetRowKey(this.storageClient.GetNextRowKey(incident.PartitionKey));

			incident = this.storageClient.PersistNewIncident(incident);

			var title = $"INCIDENT DECLARED #{ incident.FriendlyId }";
			var messageText = $"Declared Timestamp: { incident.DeclaredDateTimeUtc } UTC\n"
				+ $"Reported By: @{ incident.DeclaredBy }\n"
				+ $"Channel: { incident.ChannelName }\n"
				+ $"Description: { incident.Title }\n";

			this.SendIncidentChannelMessage(title, messageText, "danger");

			return incident;
		}

		internal Incident ResolveIncident(string resolvedBy, string incidentChannelName)
		{
			var incident = this.storageClient.GetIncidentByChannel(incidentChannelName);

			if (incident == null || incident.ResolvedDateTimeUtc != null)
			{
				return null;
			}

			incident.MarkAsResolved(resolvedBy);

			this.storageClient.UpdateIncident(incident);

			var title = $"INCIDENT RESOLVED #{ incident.FriendlyId }";
			var messageText = $"Declared Timestamp: { incident.DeclaredDateTimeUtc } UTC\n"
							+ $"Resolved Timestamp: { incident.ResolvedDateTimeUtc } UTC\n"
							+ $"Resolved By: @{ incident.ResolvedBy }\n"
							+ $"Channel: { incident.ChannelName }\n"
							+ $"Description: { incident.Title }\n";

			this.SendIncidentChannelMessage(title, messageText, "good");

			return incident;
		}

		internal Incident CloseIncident(string resolvedBy, string incidentChannelName)
		{
			var incident = this.storageClient.GetIncidentByChannel(incidentChannelName);

			if (incident?.ResolvedDateTimeUtc == null || incident.ClosedDateTimeUtc != null)
			{
				return null;
			}

			incident.MarkAsClosed(resolvedBy);

			this.storageClient.UpdateIncident(incident);

			var title = $"INCIDENT CLOSED #{ incident.FriendlyId }";
			var messageText = $"Declared Timestamp: { incident.DeclaredDateTimeUtc } UTC\n"
							+ $"Resolved Timestamp: { incident.ResolvedDateTimeUtc } UTC\n"
							+ $"Closed Timestamp: { incident.ClosedDateTimeUtc } UTC\n"
							+ $"Closed By: @{ incident.ClosedBy }\n"
							+ $"Channel: { incident.ChannelName }\n" + $"Description: { incident.Title }\n";

			this.SendIncidentChannelMessage(title, messageText, "#439FE0");

			return incident;
		}


		private void SendIncidentChannelMessage(string title, string messageText, string messageColor)
		{
			var chatHub = new SlackChatHub { Id = this.MainIncidentChannel };
			var attachement = new SlackAttachment { Title = title, Text = messageText, ColorHex = messageColor };
			var message = new BotMessage { ChatHub = chatHub, Attachments = new List<SlackAttachment> { attachement } };

			this.SlackConnection.Say(message);
		}
	}
}
