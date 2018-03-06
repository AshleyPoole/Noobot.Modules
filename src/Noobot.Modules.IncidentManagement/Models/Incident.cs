using System;

using Microsoft.WindowsAzure.Storage.Table;

namespace Noobot.Modules.IncidentManagement.Models
{
	internal class Incident : TableEntity
	{
		public Incident(string incidentTitle, Channel channel, string declaredBy)
		{
			this.Id = Guid.NewGuid();
			this.PartitionKey = DateTime.UtcNow.ToString("yyyy-MM-dd");

			this.ChannelId = channel.Id;
			this.ChannelName = channel.Name;
			this.Title = incidentTitle;
			this.DeclaredBy = declaredBy;
			this.DeclaredDateTimeUtc = DateTime.UtcNow;

			this.Resolved = false;
			this.Closed = false;
		}

		public Incident()
		{
		}

		public Guid Id { get; set; }

		public string Title { get; set; }

		public string ChannelId { get; set; }

		public string ChannelName { get; set; }

		public string DeclaredBy { get; set; }

		public DateTime DeclaredDateTimeUtc { get; set; }

		public bool Resolved { get; set; }

		public string ResolvedBy { get; set; }

		public DateTime? ResolvedDateTimeUtc { get; set; }

		public string PostmortermLink { get; set; }

		public string PostmortermAddedBy { get; set; }

		public DateTime? PostmortemAddedDateTimeUtc { get; set; }

		public bool Closed { get; set; }

		public string ClosedBy { get; set; }

		public DateTime? ClosedDateTimeUtc { get; set; }

		public string FriendlyId => $"{this.PartitionKey}-{this.RowKey}";

		public string FriendlyStatus
		{
			get
			{
				var incidentStatus = "UNKNOWN";

				if (!this.Resolved && !this.Closed)
				{
					incidentStatus = "IN-PROGRESS";
				}

				if (this.Resolved && !this.Closed)
				{
					incidentStatus = "RESOLVED";
				}

				if (this.Resolved && this.Closed)
				{
					incidentStatus = "CLOSED";
				}

				return incidentStatus;
			}
		}

		public void MarkAsResolved(string resolvedBy)
		{
			this.ResolvedDateTimeUtc = DateTime.UtcNow;
			this.ResolvedBy = resolvedBy;
			this.Resolved = true;
		}

		public void AddPostmortem(string addedBy, string postmortemLink)
		{
			this.PostmortemAddedDateTimeUtc = DateTime.UtcNow;
			this.PostmortermAddedBy = addedBy;
			this.PostmortermLink = postmortemLink;
		}

		public void MarkAsClosed(string closedBy)
		{
			this.ClosedDateTimeUtc = DateTime.UtcNow;
			this.ClosedBy = closedBy;
			this.Closed = true;
		}

		public void SetRowKey(int rowKey)
		{
			this.RowKey = rowKey.ToString();
		}
	}
}
