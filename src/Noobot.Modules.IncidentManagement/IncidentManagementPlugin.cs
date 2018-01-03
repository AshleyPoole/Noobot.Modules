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

		private string[] WarRooms { get; set; }

		public IncidentManagementPlugin(IConfigReader configReader)
		{
			this.configReader = configReader;
			this.storageClient = new IncidentManagementStorageClient(configReader);
		}

		public void Start()
		{
			this.MainIncidentChannel = this.configReader.GetConfigEntry<string>("incident:mainChannel");

			this.WarRooms = this.configReader.GetConfigEntry<string>("incident:warRooms").Split(',');

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
			return this.SlackConnection.GetChannels().Result.FirstOrDefault(x => x.Id == channel)?.Name
				.Replace("#", string.Empty);
		}

		internal Incident DeclareNewIncident(string incidentText, string reportedByUser)
		{
			var assignedWarRoomChannel = this.GetAvailableWarRoomChannel();

			if (assignedWarRoomChannel == null)
			{
				return null;
			}

			var incident = new Incident(incidentText, assignedWarRoomChannel, reportedByUser);
			incident.SetRowKey(this.storageClient.GetNextRowKey(incident.PartitionKey));

			incident = this.storageClient.PersistNewIncident(incident);

			this.SetChannelPurposeBasedOnIncidentStatus(incident);
			this.SetChannelTopicBasedOnIncidentStatus(incident);

			var title = $"INCIDENT DECLARED #{ incident.FriendlyId }";
			this.SendWarRoomIncidentChannelMessage(incident.ChannelName, this.GetNewIncidentTextForWarRoomChannel(incident));
			this.SendMainIncidentChannelMessage(title, this.GetNewIncidentTextForMainIncidentChannel(incident), Configuration.UnresolvedIncidentColor);

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

			this.SetChannelPurposeBasedOnIncidentStatus(incident);
			this.SendMainIncidentChannelMessage(title, messageText, Configuration.ResolvedIncidentColor);

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

			this.SetChannelPurposeBasedOnIncidentStatus(incident);
			this.SetChannelTopicBasedOnIncidentStatus(incident);
			this.SendMainIncidentChannelMessage(title, messageText, Configuration.ClosedIncidentColor);

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

		private void SendMainIncidentChannelMessage(string title, string messageText, string messageColor)
		{
			var chatHub = new SlackChatHub { Id = this.MainIncidentChannel };
			var attachement = new SlackAttachment { Title = title, Text = messageText, ColorHex = messageColor };
			var message = new BotMessage { ChatHub = chatHub, Attachments = new List<SlackAttachment> { attachement } };

			this.SlackConnection.Say(message);
		}

		private void SendWarRoomIncidentChannelMessage(string warRoomChannelName, string messageText)
		{
			var chatHub = new SlackChatHub { Id = warRoomChannelName };
			var message = new BotMessage { ChatHub = chatHub, Text = messageText };

			this.SlackConnection.Say(message);
		}

		private string GetNewIncidentTextForMainIncidentChannel(Incident incident)
		{
			return $"Declared Timestamp: { incident.DeclaredDateTimeUtc } UTC\n"
				+ $"Reported By: @{ incident.DeclaredBy }\n"
				+ $"Bound To Channel: #{ incident.ChannelName }\n"
				+ $"Description: { incident.Title }";
		}

		private string GetNewIncidentTextForWarRoomChannel(Incident incident)
		{
			return $"Incident #{ incident.FriendlyId } regarding '{ incident.Title }' has been declared by @{ incident.DeclaredBy } and bound to this channel.\n"
				+ "Good luck with this incident and remember to add people to the channel that might be able to help.";
		}

		private string GetResolvedIncidentTextWithoutIncidentId(Incident incident)
		{
			return $"Declared Timestamp: { incident.DeclaredDateTimeUtc } UTC\n"
				+ $"Resolved Timestamp: { incident.ResolvedDateTimeUtc } UTC\n"
				+ $"Resolved By: @{ incident.ResolvedBy }\n"
				+ $"Channel: #{ incident.ChannelName }\n"
				+ $"Description: { incident.Title }";
		}

		private string GetClosedIncidentTextWithoutIncidentId(Incident incident)
		{
			return $"Declared Timestamp: { incident.DeclaredDateTimeUtc } UTC\n"
					+ $"Resolved Timestamp: { incident.ResolvedDateTimeUtc } UTC\n"
					+ $"Closed Timestamp: { incident.ClosedDateTimeUtc } UTC\n"
					+ $"Closed By: @{ incident.ClosedBy }\n"
					+ $"Channel: #{ incident.ChannelName }\n"
					+ $"Description: { incident.Title }";
		}

		private void SetChannelPurposeBasedOnIncidentStatus(Incident incident)
		{
			if (incident.Resolved && incident.Closed)
			{
				var result = this.SlackConnection.SetChannelPurpose(
					incident.ChannelId,
					$"Incident Warroom -- No active incident bound").Result;
			}
			else
			{
				var result = this.SlackConnection.SetChannelPurpose(
					incident.ChannelId,
					$"INCIDENT #{ incident.FriendlyId } -- { incident.FriendlyStatus } -- { incident.Title }").Result;
			}
		}

		private void SetChannelTopicBasedOnIncidentStatus(Incident incident)
		{
			if (incident.Resolved && incident.Closed)
			{
				var result = this.SlackConnection.SetChannelTopic(
					incident.ChannelId,
					string.Empty).Result;
			}
			else
			{
				var result = this.SlackConnection.SetChannelTopic(
					incident.ChannelId,
					$"INCIDENT #{ incident.FriendlyId }").Result;
			}
		}

		private Channel GetAvailableWarRoomChannel()
		{
			foreach (var warRoomName in this.WarRooms)
			{
				var lastInstanceForChannel = this.storageClient.GetIncidentByChannel(warRoomName);
				if (lastInstanceForChannel == null || lastInstanceForChannel.Closed)
				{
					return new Channel(
						this.SlackConnection.GetChannels().Result.FirstOrDefault(x => x.Name == $"#{ warRoomName }")?.Id,
						warRoomName);
				}
			}

			return null;
		}
	}
}
