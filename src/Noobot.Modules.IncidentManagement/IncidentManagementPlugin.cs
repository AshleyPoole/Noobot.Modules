using System;
using System.Collections.Generic;
using System.Linq;

using Noobot.Core.Configuration;
using Noobot.Core.Plugins;
using Noobot.Modules.IncidentManagement.Models;

using SlackConnector;
using SlackConnector.Models;
using Noobot.Core.MessagingPipeline.Response;

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
			var messageText = this.GetUnresolvedIncidentTextWithoutIncidentId(incident);

			this.SendIncidentChannelMessage(title, messageText, Configuration.UnresolvedIncidentColor);

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
			var messageText = this.GetResolvedIncidentTextWithoutIncidentId(incident);

			this.SendIncidentChannelMessage(title, messageText, Configuration.ResolvedIncidentColor);

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
			var messageText = this.GetClosedIncidentTextWithoutIncidentId(incident);

			this.SendIncidentChannelMessage(title, messageText, Configuration.ClosedIncidentColor);

			return incident;
		}

		internal List<Attachment> GetOpenIncidents()
		{
			var openIncidents = this.storageClient.GetOpenIncidents().OrderBy(x => x.DeclaredDateTimeUtc).ToList();
			var attachments = this.GetAttachmentsForUnresolvedIncidents(openIncidents);

			attachments.AddRange(this.GetAttachmentsForResolvedIncidents(openIncidents));

			return attachments;
		}

		internal List<Attachment> GetRecentIncidents()
		{
			var recentIncidents = this.storageClient.GetRecentIncidents().OrderBy(x => x.DeclaredDateTimeUtc).ToList();
			var attachments = this.GetAttachmentsForUnresolvedIncidents(recentIncidents);

			attachments.AddRange(this.GetAttachmentsForResolvedIncidents(recentIncidents));
			attachments.AddRange(this.GetAttachmentsForClosedIncidents(recentIncidents));

			return attachments;
		}

		private List<Attachment> GetAttachmentsForUnresolvedIncidents(List<Incident> incidents)
		{
			return incidents.Where(x => x.ResolvedDateTimeUtc == null).Select(
				incident => this.GenerateAttachment(incident, Configuration.UnresolvedIncidentColor)).ToList();
		}

		private List<Attachment> GetAttachmentsForResolvedIncidents(List<Incident> incidents)
		{
			return incidents.Where(x => x.ResolvedDateTimeUtc != null).Select(
				incident => this.GenerateAttachment(incident, Configuration.ResolvedIncidentColor)).ToList();
		}

		private List<Attachment> GetAttachmentsForClosedIncidents(List<Incident> incidents)
		{
			return incidents.Where(x => x.ResolvedDateTimeUtc != null).Select(
				incident => this.GenerateAttachment(incident, Configuration.ClosedIncidentColor)).ToList();
		}

		private Attachment GenerateAttachment(Incident incident, string attachmentColor)
		{
			return new Attachment
					{
						Title = $"INCIDENT { incident.FriendlyId }",
						Text = this.GetResolvedIncidentTextWithoutIncidentId(incident),
						Color = attachmentColor
			};
		}

		private void SendIncidentChannelMessage(string title, string messageText, string messageColor)
		{
			var chatHub = new SlackChatHub { Id = this.MainIncidentChannel };
			var attachement = new SlackAttachment { Title = title, Text = messageText, ColorHex = messageColor };
			var message = new BotMessage { ChatHub = chatHub, Attachments = new List<SlackAttachment> { attachement } };

			this.SlackConnection.Say(message);
		}

		private string GetUnresolvedIncidentTextWithoutIncidentId(Incident incident)
		{
			return $"Declared Timestamp: { incident.DeclaredDateTimeUtc } UTC\n"
				+ $"Reported By: @{ incident.DeclaredBy }\n"
				+ $"Channel: { incident.ChannelName }\n"
				+ $"Description: { incident.Title }";
		}

		private string GetResolvedIncidentTextWithoutIncidentId(Incident incident)
		{
			return $"Declared Timestamp: { incident.DeclaredDateTimeUtc } UTC\n"
				+ $"Resolved Timestamp: { incident.ResolvedDateTimeUtc } UTC\n"
				+ $"Resolved By: @{ incident.ResolvedBy }\n"
				+ $"Channel: { incident.ChannelName }\n"
				+ $"Description: { incident.Title }";
		}

		private string GetClosedIncidentTextWithoutIncidentId(Incident incident)
		{
			return $"Declared Timestamp: { incident.DeclaredDateTimeUtc } UTC\n"
					+ $"Resolved Timestamp: { incident.ResolvedDateTimeUtc } UTC\n"
					+ $"Closed Timestamp: { incident.ClosedDateTimeUtc } UTC\n"
					+ $"Closed By: @{ incident.ClosedBy }\n"
					+ $"Channel: { incident.ChannelName }\n"
					+ $"Description: { incident.Title }";
		}
	}
}
