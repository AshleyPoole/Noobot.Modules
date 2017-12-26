using System;
using System.Collections.Generic;
using System.Linq;

using NewRelic;
using NewRelic.Models;

using Noobot.Core.Configuration;
using Noobot.Core.Plugins;

namespace Noobot.Modules.NewRelic
{
	public class NewRelicPlugin : IPlugin
	{
		private readonly IConfigReader configReader;

		private string apiKey;

		public NewRelicPlugin(IConfigReader configReader)
		{
			this.configReader = configReader;
		}

		public void Start()
		{
			this.apiKey = this.configReader.GetConfigEntry<string>("newrelic:apikey");
		}

		public void Stop()
		{
		}

		internal NewRelicRestClient GetNewRelicClient()
		{
			return new NewRelicRestClient(this.apiKey);
		}

		internal static bool ApplicationTargetedCommandMisformed(string message)
		{
			return message.Split(" ", StringSplitOptions.RemoveEmptyEntries).Length != 4;
		}

		internal string GetApplicationsText()
		{
			var applications = this.FetchApplications();

			var applicationsMessage = string.Empty;

			foreach (var application in applications)
			{
				if (!string.IsNullOrWhiteSpace(applicationsMessage))
				{
					applicationsMessage += "\n";
				}

				applicationsMessage += $"ID: {application.Id}    Name: {application.Name}    HealthStatus: {application.HealthStatus}";
			}

			return $"There are {applications.Count} applications in NewRelic. Those are:\n {applicationsMessage}";
		}

		internal string GetApplicationHealthText(Application application)
		{
			var applicationMessage = $"HealthStatus: {application.HealthStatus}\n"
									+ $"Language: {application.Language}\n"
									+ $"IsReporting: {application.IsReporting}\n"
									+ $"LastReportedDate: {application.LastReportedDate}\n"
									+ $"ResponseTime: {application.Summary.ResponseTime} ms\n"
									+ $"ErrorRate: {application.Summary.ErrorRate}\n"
									+ $"Apendex: {application.Summary.ApdexScore} / {application.Summary.ApdexTarget} target\n";

			return $"NewRelic application summary for {application.Id} ({application.Name}):\n {applicationMessage}";
		}

		internal string GetApplicationsSummaryText()
		{
			var applicationsMessage = string.Empty;

			foreach (var application in this.FetchApplications())
			{
				if (!string.IsNullOrWhiteSpace(applicationsMessage))
				{
					applicationsMessage += "\n\n";
				}

				applicationsMessage += $"*** {application.Id} ({application.Name}) ***\n"
										+ $"HealthStatus: {application.HealthStatus}\n"
										+ $"Language: {application.Language}\n"
										+ $"IsReporting: {application.IsReporting}\n"
										+ $"LastReportedDate: {application.LastReportedDate}\n"
										+ $"ResponseTime: {application.Summary.ResponseTime} ms\n"
										+ $"ErrorRate: {application.Summary.ErrorRate}\n"
										+ $"Apendex: {application.Summary.ApdexScore} / {application.Summary.ApdexTarget} target";
			}

			return $"NewRelic summary for all applications:\n {applicationsMessage}";
		}

		internal Application GetApplicationFromTargetedText(string targetedText)
		{
			var applicationIdText = targetedText.Split(" ", StringSplitOptions.RemoveEmptyEntries)[3];
			var applicationId = int.Parse(applicationIdText);

			var applications = this.FetchApplications();
			return applications.FirstOrDefault(
				x => x.Id == applicationId);
		}

		internal string GetSummaryMetricsText(int applicationId)
		{
			var newRelic = this.GetNewRelicClient();
			var metrics = newRelic.GetSummaryMetrics(applicationId).Result;

			var metricsMessage = string.Empty;

			foreach (var metric in metrics)
			{
				if (!string.IsNullOrWhiteSpace(metricsMessage))
				{
					metricsMessage += "\n";
				}

				metricsMessage += $"MetricName: {metric.Name}    BeginTime: {metric.BeginTime}    EndTime: {metric.EndTime}    MetricValue: {metric.FormattedMetricValue}";
			}

			return $"NewRelic metrics summary for {applicationId}\n{metricsMessage}";
		}

		private List<Application> FetchApplications()
		{
			var newRelic = this.GetNewRelicClient();
			return newRelic.GetApplications().Result.Applications;
		}
	}
}
