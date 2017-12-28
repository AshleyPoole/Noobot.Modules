using System;

using Microsoft.WindowsAzure.Storage.Table;

namespace Noobot.Modules.IncidentManagement.Models
{
	public class Incident : TableEntity
	{
		public Incident(string incidentTitle, string declaredInChannelName, string declaredBy)
		{
			this.Id = Guid.NewGuid();
			this.PartitionKey = DateTime.UtcNow.ToString("yyyy-MM-dd");

			this.ChannelName = declaredInChannelName;
			this.Title = incidentTitle;
			this.DeclaredBy = declaredBy;
			this.DeclaredDateTimeUtc = DateTime.UtcNow;
		}

		public Incident()
		{
		}

		public Guid Id { get; set; }

		public string ChannelName { get; set; }

		public string Title { get; set; }

		public string DeclaredBy { get; set; }

		public DateTime DeclaredDateTimeUtc { get; set; }

		public string ResolvedBy { get; set; }

		public DateTime? ResolvedDateTimeUtc { get; set; }

		public string ClosedBy { get; set; }

		public DateTime? ClosedDateTimeUtc { get; set; }

		public string FriendlyId => $"{this.PartitionKey}-{this.RowKey}";

		public void MarkAsResolved(string resolvedBy)
		{
			this.ResolvedDateTimeUtc = DateTime.UtcNow;
			this.ResolvedBy = resolvedBy;
		}

		public void MarkAsClosed(string closedBy)
		{
			this.ClosedDateTimeUtc = DateTime.UtcNow;
			this.ClosedBy = closedBy;
		}

		public void SetRowKey(int rowKey)
		{
			this.RowKey = rowKey.ToString();
		}
	}
}
