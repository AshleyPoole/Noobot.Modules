using System.Collections.Generic;
using System.Linq;

using Common.Logging;

using Noobot.Core.MessagingPipeline.Middleware;
using Noobot.Core.MessagingPipeline.Middleware.ValidHandles;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;

namespace Noobot.Modules.NewRelic
{
	public class NewRelicMiddleware : MiddlewareBase
	{
		private readonly NewRelicPlugin newRelicPlugin;

		private readonly string applicationSummaryHelpText = $"`{Configuration.CommandPrefix} application summary 1711605`";

		private readonly string applicationAllSummaryHelpText = $"`{Configuration.CommandPrefix} applications summary`";

		private readonly string applicationMetricsHelpText = $"`{Configuration.CommandPrefix} application metrics 1711605`";

		public NewRelicMiddleware(IMiddleware next, NewRelicPlugin newRelicPlugin, ILog log)
			: base(next)
		{
			this.newRelicPlugin = newRelicPlugin;
			this.HandlerMappings = new[]
									{
										new HandlerMapping
										{
											ValidHandles = ExactMatchHandle.For($"{Configuration.CommandPrefix} applications"),
											EvaluatorFunc = this.ApplicationsHandler,
											Description = $"Gets applications being monitored in NewRelic. '{Configuration.CommandPrefix} applications'",
											VisibleInHelp = true
										},
										new HandlerMapping
										{
											ValidHandles = StartsWithHandle.For($"{Configuration.CommandPrefix} application summary"),
											EvaluatorFunc = this.ApplicationSummaryHandler,
											Description = $"Gets application summary from NewRelic for the requested application id. {this.applicationSummaryHelpText}",
											VisibleInHelp = true
										},
										new HandlerMapping
										{
											ValidHandles = ExactMatchHandle.For($"{Configuration.CommandPrefix} applications summary"),
											EvaluatorFunc = this.AllApplicationSummaryHandler,
											Description = $"Gets summary for all applications from NewRelic. {this.applicationAllSummaryHelpText}",
											VisibleInHelp = true
										},
										new HandlerMapping
										{
											ValidHandles = StartsWithHandle.For($"{Configuration.CommandPrefix} application metrics"),
											EvaluatorFunc = this.ApplicationMetricsHandler,
											Description = $"Gets application metrics from NewRelic for the requested application id. {this.applicationMetricsHelpText}",
											VisibleInHelp = false
										},
									};
		}

		private IEnumerable<ResponseMessage> ApplicationsHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			incomingMessage.IndicateTypingOnChannel();

			var newRelic = this.newRelicPlugin.GetNewRelicClient();
			var applications = newRelic.GetApplications().Result.Applications;

			var applicationsMessage = string.Empty;

			foreach (var application in applications)
			{
				if (!string.IsNullOrWhiteSpace(applicationsMessage))
				{
					applicationsMessage += "\n";
				}

				applicationsMessage += $"ID: {application.Id}    Name: {application.Name}    HealthStatus: {application.HealthStatus}";
			}

			yield return incomingMessage.ReplyToChannel($"There are {applications.Count} applications in NewRelic. Those are:\n {applicationsMessage}");
		}

		private IEnumerable<ResponseMessage> ApplicationSummaryHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			incomingMessage.IndicateTypingOnChannel();

			if (!NewRelicPlugin.ApplicationTargetedCommandWellFormatted(incomingMessage.TargetedText))
			{
				yield return incomingMessage.ReplyToChannel($"NewRelic application summary was not formatted correctly. Help: {this.applicationSummaryHelpText}");
				yield break;
			}

			var newRelic = this.newRelicPlugin.GetNewRelicClient();
			var applications = newRelic.GetApplications().Result.Applications;

			var application = applications.FirstOrDefault(
				x => x.Id == this.newRelicPlugin.GetAccountIdFromApplicationTargeted(incomingMessage.TargetedText));
			if (application == null)
			{
				yield return incomingMessage.ReplyToChannel($"No NewRelic application could be found with the ID listed. Help: {this.applicationSummaryHelpText}");
				yield break;
			}

			var applicationMessage = $"HealthStatus: {application.HealthStatus}\n"
				+ $"Language: {application.Language}\n"
				+ $"IsReporting: {application.IsReporting}\n"
				+ $"LastReportedDate: {application.LastReportedDate}\n"
				+ $"ResponseTime: {application.Summary.ResponseTime} ms\n"
				+ $"ErrorRate: {application.Summary.ErrorRate}\n"
				+ $"Apendex: {application.Summary.ApdexScore} / {application.Summary.ApdexTarget} target\n";

			yield return incomingMessage.ReplyToChannel($"NewRelic application summary for {application.Id} ({application.Name}):\n {applicationMessage}");
		}

		private IEnumerable<ResponseMessage> AllApplicationSummaryHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			incomingMessage.IndicateTypingOnChannel();

			var newRelic = this.newRelicPlugin.GetNewRelicClient();
			var applications = newRelic.GetApplications().Result.Applications;

			var applicationsMessage = string.Empty;

			foreach (var application in applications)
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

			yield return incomingMessage.ReplyToChannel($"NewRelic summary for all applications:\n {applicationsMessage}");
		}

		private IEnumerable<ResponseMessage> ApplicationMetricsHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			incomingMessage.IndicateTypingOnChannel();
			yield return incomingMessage.ReplyToChannel($"Sorry this command isn't supported yet.");
			yield break;

			if (!NewRelicPlugin.ApplicationTargetedCommandWellFormatted(incomingMessage.TargetedText))
			{
				yield return incomingMessage.ReplyToChannel($"NewRelic application metrics command was not formatted correctly. Help: {this.applicationMetricsHelpText}");
				yield break;
			}

			var newRelic = this.newRelicPlugin.GetNewRelicClient();

			var applicationId = this.newRelicPlugin.GetAccountIdFromApplicationTargeted(incomingMessage.TargetedText);

			var applicationMetrics = newRelic.GetSummaryMetrics(applicationId).Result;
			if (applicationMetrics == null)
			{
				yield return incomingMessage.ReplyToChannel($"No NewRelic application could be found with the ID listed. Help: {this.applicationMetricsHelpText}");
				yield break;
			}

			var metricsMessage = string.Empty;

			foreach (var metric in applicationMetrics)
			{
				if (!string.IsNullOrWhiteSpace(metricsMessage))
				{
					metricsMessage += "\n";
				}

				metricsMessage += $"MetricName: {metric.Name}    BeginTime: {metric.BeginTime}    EndTime: {metric.EndTime}    MetricValue: {metric.FormattedMetricValue}";
			}

			yield return incomingMessage.ReplyToChannel($"NewRelic metrics summary for {applicationId}");
		}

	}
}
