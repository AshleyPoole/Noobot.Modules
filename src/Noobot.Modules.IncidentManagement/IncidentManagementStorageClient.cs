using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

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
					return int.Parse(incidents.Results.OrderByDescending(x => x.RowKey).First().RowKey) + 1;
					
				}

				return 1;

			} while (token != null);
		}

		internal Incident PersistNewIncident(Incident incident)
		{
			var insertOperation = TableOperation.Insert(incident);
			return (Incident)this.incidentTable.ExecuteAsync(insertOperation).Result.Result;
		}

		internal Incident GetIncidentByChannel(string channelName)
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

		internal Incident UpdateIncident(Incident incident)
		{
			var updateOperation = TableOperation.Replace(incident);
			return (Incident)this.incidentTable.ExecuteAsync(updateOperation).Result.Result;
		}
	}
}
