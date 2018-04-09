using System;
using System.Collections.Generic;
using System.Linq;

using Common.Logging;

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
		public string PostmortemTemplateLink { get; private set; }

		private string mainIncidentChannel;

		private readonly IConfigReader configReader;

		private readonly ILog log;

		private readonly IncidentManagementStorageClient storageClient;

		private ISlackConnection SlackConnection { get; set; }

		private string[] WarRooms { get; set; }

		public IncidentManagementPlugin(IConfigReader configReader, ILog log)
		{
			this.configReader = configReader;
			this.log = log;
			this.storageClient = new IncidentManagementStorageClient(configReader);
		}

		public void Start()
		{
			this.mainIncidentChannel = this.configReader.GetConfigEntry<string>("incident:mainChannel");
			this.PostmortemTemplateLink = this.configReader.GetConfigEntry<string>("incident:postmortemTemplateLink");

			this.WarRooms = this.configReader.GetConfigEntry<string>("incident:warRooms").Split(',');

			var connector = new SlackConnector.SlackConnector();
			this.SlackConnection = connector.Connect(this.configReader.SlackApiKey).Result;

			this.log.Info("(IncidentModule) started up.");
		}

		public void Stop()
		{
		}

		internal bool IncidentCommandUserInputWellFormatted(string message)
		{
			return message.Split(" ", StringSplitOptions.RemoveEmptyEntries).Length >= 3;
		}

		internal Incident DeclareNewIncident(string incidentText, string reportedByUser)
		{
			var assignedWarRoomChannel = this.GetAvailableWarRoomChannel();

			if (assignedWarRoomChannel == null)
			{
				this.log.Info($"(IncidentModule) Found no available channels for a new incident being declared by {reportedByUser}.");
				return null;
			}

			this.log.Info($"(IncidentModule) Found channel {assignedWarRoomChannel} for new incident declared by {reportedByUser}");

			var incident = new Incident(incidentText, assignedWarRoomChannel, reportedByUser);
			incident.SetRowKey(this.storageClient.GetNextRowKey(incident.PartitionKey));

			incident = this.storageClient.PersistNewIncident(incident);

			this.log.Info($"(IncidentModule) Declared new incident for {reportedByUser} with incidentId:{incident.Id}");

			this.SetChannelPurposeBasedOnIncidentStatus(incident);
			this.SetChannelTopicBasedOnIncidentStatus(incident);

			this.log.Info($"(IncidentModule) Warroom {incident.ChannelName} topic and purpose has been updated for incidentId:{incident.Id}");

			var title = $"INCIDENT DECLARED #{ incident.FriendlyId }";
			this.SendWarRoomIncidentChannelMessage(incident.ChannelName, TextHelper.GetNewIncidentTextForWarRoomChannel(incident));
			this.log.Info($"(IncidentModule) Warrom {incident.ChannelName} has been updated with incident text for incidentId:{incident.Id}");

			this.SendMainIncidentChannelMessage(
				title,
				TextHelper.GetNewIncidentTextForMainIncidentChannel(incident),
				Configuration.UnresolvedIncidentColor);

			this.log.Info($"(IncidentModule) Sent notification to main incidents channel with summary for incidentId:{incident.Id}");

			return incident;
		}

		internal Incident ResolveIncident(string resolvedBy, string incidentChannelId)
		{
			var incident = this.storageClient.GetIncidentByChannelId(incidentChannelId);

			if (incident == null || incident.ResolvedDateTimeUtc != null)
			{
				return null;
			}

			incident.MarkAsResolved(resolvedBy);

			this.storageClient.UpdateIncident(incident);

			var title = $"INCIDENT RESOLVED #{ incident.FriendlyId }";
			var messageText = TextHelper.GetResolvedIncidentTextWithoutIncidentId(incident);

			this.SetChannelPurposeBasedOnIncidentStatus(incident);
			this.SendMainIncidentChannelMessage(title, messageText, Configuration.ResolvedIncidentColor);

			return incident;
		}

		internal Incident UpdateIncidentWithPostmortem(string postmortemLink, string addedByUser, string incidentChannelId)
		{
			var incident = this.storageClient.GetIncidentByChannelId(incidentChannelId);

			if (incident == null || incident.ClosedDateTimeUtc != null)
			{
				return null;
			}

			incident.AddPostmortem(addedByUser, postmortemLink);

			this.storageClient.UpdateIncident(incident);

			var title = $"INCIDENT POSTMORTEM ADDED #{ incident.FriendlyId }";
			var messageText = TextHelper.GetIncidentPostmortemTextWithoutIncidentId(incident);

			this.SendMainIncidentChannelMessage(title, messageText, Configuration.PostmortemIncidentColor);

			return incident;
		}

		internal Incident CloseIncident(string resolvedBy, string incidentChannelId)
		{
			var incident = this.storageClient.GetIncidentByChannelId(incidentChannelId);

			if (incident?.ResolvedDateTimeUtc == null || incident.ClosedDateTimeUtc != null || incident.PostmortemAddedDateTimeUtc == null)
			{
				return null;
			}

			incident.MarkAsClosed(resolvedBy);

			this.storageClient.UpdateIncident(incident);

			var title = $"INCIDENT CLOSED #{ incident.FriendlyId }";
			var messageText = TextHelper.GetClosedIncidentTextWithoutIncidentId(incident);

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
			return incidents.Where(x => x.ClosedDateTimeUtc != null).Select(
				incident => this.GenerateAttachment(incident, Configuration.ClosedIncidentColor)).ToList();
		}

		private Attachment GenerateAttachment(Incident incident, string attachmentColor)
		{
			return new Attachment
					{
						Title = $"INCIDENT { incident.FriendlyId }",
						Text = TextHelper.GetResolvedIncidentTextWithoutIncidentId(incident),
						Color = attachmentColor
			};
		}

		private void SendMainIncidentChannelMessage(string title, string messageText, string messageColor)
		{
			var chatHub = new SlackChatHub { Id = this.mainIncidentChannel };
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
					Configuration.Whitespace).Result;
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
			var channels = this.SlackConnection.GetChannels().Result;

			foreach (var warRoomName in this.WarRooms)
			{
				var lastInstanceForChannel = this.storageClient.GetIncidentByChannelName(warRoomName);
				if (lastInstanceForChannel == null || lastInstanceForChannel.Closed)
				{
					var channelNameWithHash = $"#{ warRoomName }";
					return new Channel(
						channels.FirstOrDefault(x => x.Name == channelNameWithHash)?.Id,
						warRoomName);
				}
			}

			return null;
		}
	}
}
