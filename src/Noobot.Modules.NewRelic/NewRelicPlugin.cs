using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using NewRelic;
using NewRelic.Models;

using Noobot.Core.Configuration;
using Noobot.Core.MessagingPipeline.Response;
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

		internal static bool ApplicationDetailTargetedCommandMisformed(string message)
		{
			return message.Split(" ", StringSplitOptions.RemoveEmptyEntries).Length != 4;
		}

		internal List<Attachment> GetApplications()
		{
			var applications = this.FetchApplications();
			var applicationAttachments = new List<Attachment>();

			foreach (var application in applications)
			{
				var attachmentFields = new List<AttachmentField>
										{
											new AttachmentField
											{
												IsShort = true,
												Title = nameof(application.Id),
												Value = application.Id.ToString()
											},
											new AttachmentField { IsShort = true, Title = nameof(application.Name), Value = application.Name },
											new AttachmentField
											{
												IsShort = true,
												Title = nameof(application.IsReporting),
												Value = application.IsReporting
											},
											new AttachmentField
											{
												IsShort = true,
												Title = nameof(application.HealthStatus),
												Value = application.HealthStatus
											}
										};

				applicationAttachments.Add(
					new Attachment
					{
						AttachmentFields = attachmentFields,
						Color = GetAttachmentColourForHealth(application.HealthStatus)
					});
			}

			return applicationAttachments;
		}

		internal List<Attachment> GetAllApplicationsDetails()
		{
			return this.GetFormatedApplicationsDetails(this.FetchApplications());
		}

		internal List<Attachment> GetUnhealthyApplicationsDetail()
		{
			var allApplications = this.FetchApplications();
			var unhealthyApplications = allApplications.Where(x => x.HealthStatus != Configuration.NewRelicGoodStatus).ToList();

			return this.GetFormatedApplicationsDetails(unhealthyApplications);
		}

		internal List<Attachment> GetApplicationDetailsFiltered(string applicationName)
		{
			const string WildcardCharacter = "%";
			List<Application> filteredApplications;

			var applicationNameForChecking = applicationName.Replace(WildcardCharacter, string.Empty).ToLower();
			var allApplications = this.FetchApplications();

			if (applicationName.StartsWith(WildcardCharacter))
			{
				filteredApplications =
					allApplications.Where(x => x.Name.ToLower().EndsWith(applicationNameForChecking)).ToList();
			}
			else if (applicationName.EndsWith(WildcardCharacter))
			{
				filteredApplications =
					allApplications.Where(x => x.Name.ToLower().StartsWith(applicationNameForChecking)).ToList();
			}
			else if (applicationName.StartsWith(WildcardCharacter) && applicationName.EndsWith(WildcardCharacter))
			{
				filteredApplications =
					allApplications.Where(x => x.Name.ToLower().Contains(applicationNameForChecking)).ToList();
			}
			else
			{
				filteredApplications = allApplications.Where(x => x.Name.ToLower() == applicationNameForChecking).ToList();
			}

			return this.GetFormatedApplicationsDetails(filteredApplications);
		}

		private List<Attachment> GetFormatedApplicationsDetails(List<Application> applications)
		{
			var applicationAttachments = new List<Attachment>();

			foreach (var application in applications)
			{
				var attachmentFields = new List<AttachmentField>
										{
											new AttachmentField
											{
												IsShort = true,
												Title = nameof(application.Id),
												Value = application.Id.ToString()
											},
											new AttachmentField { IsShort = true, Title = nameof(application.Name), Value = application.Name },
											new AttachmentField
											{
												IsShort = true,
												Title = nameof(application.Language),
												Value = application.Language
											},
											new AttachmentField
											{
												IsShort = true,
												Title = nameof(application.HealthStatus),
												Value = application.HealthStatus
											},
											new AttachmentField
											{
												IsShort = true,
												Title = nameof(application.IsReporting),
												Value = application.IsReporting
											},
											new AttachmentField
											{
												IsShort = true,
												Title = nameof(application.LastReportedDate),
												Value = $"{ application.LastReportedDate.UtcDateTime } UTC"
											},
											new AttachmentField
											{
												IsShort = true,
												Title = nameof(application.Summary.ErrorRate),
												Value = $"{application.Summary.ErrorRate}%"
											},
											new AttachmentField
											{
												IsShort = true,
												Title = nameof(application.Summary.ResponseTime),
												Value = $"{application.Summary.ResponseTime}ms"
											},
											new AttachmentField
											{
												IsShort = true,
												Title = nameof(application.Summary.ApdexScore),
												Value = $"{ application.Summary.ApdexScore} / {application.Summary.ApdexTarget} target"
											},
											new AttachmentField
											{
												IsShort = true,
												Title = $"{nameof(application.EndUserSummary.ApdexScore)} {nameof(application.EndUserSummary)}",
												Value =
													$"{application.EndUserSummary?.ApdexScore} / {application.EndUserSummary?.ApdexTarget} target"
											},
											new AttachmentField
											{
												IsShort = true,
												Title = nameof(application.Summary.Throughput),
												Value = $"{application.Summary.Throughput}rpm"
											}
										};

				applicationAttachments.Add(new Attachment
								{
									AttachmentFields = attachmentFields,
									Color = GetAttachmentColourForHealth(application.HealthStatus)
								});
			}

			return applicationAttachments;
		}

		internal Application GetApplicationFromTargetedText(string targetedText)
		{
			var applicationIdText = targetedText.Split(" ", StringSplitOptions.RemoveEmptyEntries)[3];
			var applicationId = int.Parse(applicationIdText);

			var applications = this.FetchApplications();
			return applications.FirstOrDefault(
				x => x.Id == applicationId);
		}

		internal string GetApplicationNameFromTargetedText(string targetedText)
		{
			var applicationName = targetedText.Split(" ", StringSplitOptions.RemoveEmptyEntries)[3];
			return applicationName;
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

		private NewRelicRestClient GetNewRelicClient()
		{
			return new NewRelicRestClient(this.apiKey);
		}

		private List<Application> FetchApplications()
		{
			var newRelic = this.GetNewRelicClient();
			return newRelic.GetApplications().Result.Applications;
		}

		private static string GetAttachmentColourForHealth(string health)
		{
			switch (health)
			{
				case Configuration.NewRelicGoodStatus:
					return Configuration.NewRelicGoodStatusColor;
				case Configuration.NewRelicWarningStatus:
					return Configuration.NewRelicWarningStatusColor;
				case Configuration.NewRelicBadStatus:
					return Configuration.NewRelicBadStatusColor;
				default:
					return Configuration.NewRelicUnknownStatusColor;
			}
		}
	}
}
