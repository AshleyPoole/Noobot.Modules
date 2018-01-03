using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

using Noobot.Core.Configuration;
using Noobot.Modules.IncidentManagement.Models;

namespace Noobot.Modules.IncidentManagement
{
	public class IncidentManagementStorageClient
	{
		private readonly IConfigReader configReader;

		private CloudTable incidentTable;

		public IncidentManagementStorageClient(IConfigReader configReader)
		{
			this.configReader = configReader;
			this.StartUp();
		}

		private async void StartUp()
		{
			var connectionString = this.configReader.GetConfigEntry<string>("incident:AzureConnectionString");
			var storageAccount = CloudStorageAccount.Parse(connectionString);

			var tableClient = storageAccount.CreateCloudTableClient();
			this.incidentTable = tableClient.GetTableReference("incidents");

			await this.incidentTable.CreateIfNotExistsAsync();
		}

		internal int GetNextRowKey(string incidentDateTime)
		{
			var query = new TableQuery<Incident>().Where(
				TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, incidentDateTime));

			TableContinuationToken token = null;
			do
			{
				var incidents = this.incidentTable.ExecuteQuerySegmentedAsync(query, token).Result;
				token = incidents.ContinuationToken;

				if (incidents.Results.Any())
				{
					return int.Parse(incidents.Results.OrderByDescending(x => x.DeclaredDateTimeUtc).First().RowKey) + 1;
					
				}

				return 1;

			} while (token != null);
		}

		internal Incident PersistNewIncident(Incident incident)
		{
			var insertOperation = TableOperation.Insert(incident);
			return (Incident)this.incidentTable.ExecuteAsync(insertOperation).Result.Result;
		}

		internal Incident GetIncidentByChannelName(string channelName)
		{
			var query = new TableQuery<Incident>().Where(
				TableQuery.GenerateFilterCondition(nameof(Incident.ChannelName), QueryComparisons.Equal, channelName));

			TableContinuationToken token = null;
			do
			{
				var incidents = this.incidentTable.ExecuteQuerySegmentedAsync(query, token).Result;
				token = incidents.ContinuationToken;

				return !incidents.Results.Any() ? null : incidents.Results.OrderByDescending(x => x.DeclaredDateTimeUtc).First();

			} while (token != null);
		}

		internal Incident GetIncidentByChannelId(string channelId)
		{
			var query = new TableQuery<Incident>().Where(
				TableQuery.GenerateFilterCondition(nameof(Incident.ChannelId), QueryComparisons.Equal, channelId));

			TableContinuationToken token = null;
			do
			{
				var incidents = this.incidentTable.ExecuteQuerySegmentedAsync(query, token).Result;
				token = incidents.ContinuationToken;

				return !incidents.Results.Any() ? null : incidents.Results.OrderByDescending(x => x.DeclaredDateTimeUtc).First();

			} while (token != null);
		}

		internal Incident UpdateIncident(Incident incident)
		{
			var updateOperation = TableOperation.Replace(incident);
			return (Incident)this.incidentTable.ExecuteAsync(updateOperation).Result.Result;
		}

		internal List<Incident> GetOpenIncidents()
		{
			var query = new TableQuery<Incident>().Where(
				TableQuery.GenerateFilterConditionForBool(nameof(Incident.Closed), QueryComparisons.Equal, false));

			TableContinuationToken token = null;
			do
			{
				var incidents = this.incidentTable.ExecuteQuerySegmentedAsync(query, token).Result;
				token = incidents.ContinuationToken;

				return incidents.Results;

			} while (token != null);
		}

		internal List<Incident> GetRecentIncidents()
		{
			var query = new TableQuery<Incident>().Where(
				TableQuery.GenerateFilterCondition(
					nameof(Incident.PartitionKey),
					QueryComparisons.GreaterThanOrEqual,
					DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd")));

			TableContinuationToken token = null;
			do
			{
				var incidents = this.incidentTable.ExecuteQuerySegmentedAsync(query, token).Result;
				token = incidents.ContinuationToken;

				return incidents.Results;

			} while (token != null);
		}
	}
}
