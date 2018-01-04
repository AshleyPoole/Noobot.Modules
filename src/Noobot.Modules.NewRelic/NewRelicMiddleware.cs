using System;
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

		private readonly string applicationDetailHelpText = $"`{Configuration.CommandPrefix} application detail PROD-MyApp` or `{Configuration.CommandPrefix} application detail %PartialAppName%`";

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
											Description = $"Gets applications summary from NewRelic. '{Configuration.CommandPrefix} applications'",
											VisibleInHelp = true
										},
										new HandlerMapping
										{
											ValidHandles = ExactMatchHandle.For($"{Configuration.CommandPrefix} applications detail"),
											EvaluatorFunc = this.ApplicationsDetailHandler,
											Description = $"Gets applications detail from NewRelic. {Configuration.CommandPrefix} applications detail",
											VisibleInHelp = true
										},
										new HandlerMapping
										{
											ValidHandles = ExactMatchHandle.For($"{Configuration.CommandPrefix} applications detail unhealthy"),
											EvaluatorFunc = this.UnhealthyApplicationsDetailHandler,
											Description = $"Gets applications detail from NewRelic which are unhealthy. `applications detail unhealthy`",
											VisibleInHelp = true
										},
										new HandlerMapping
										{
											ValidHandles = StartsWithHandle.For($"{Configuration.CommandPrefix} application detail"),
											EvaluatorFunc = this.ApplicationDetailHandler,
											Description = $"Gets application detail for one or more applications from NewRelic. {this.applicationDetailHelpText}",
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
			yield return incomingMessage.IndicateTypingOnChannel();

			string title;
			var applicationAttachments = new List<Attachment>();

			try
			{
				applicationAttachments = this.newRelicPlugin.GetApplications();
				title = $"There are {applicationAttachments.Count} applications in NewRelic. Those are:";

			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				title = "An exception occurred when retrieving an list of the applications from NewRelic.";
			}

			yield return incomingMessage.ReplyToChannel(title, applicationAttachments);
		}

		private IEnumerable<ResponseMessage> ApplicationsDetailHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			yield return incomingMessage.IndicateTypingOnChannel();

			string title;
			var applicationAttachments = new List<Attachment>();

			try
			{
				applicationAttachments = this.newRelicPlugin.GetAllApplicationsDetails();
				title = $"There are {applicationAttachments.Count} applications in NewRelic. Those are:";

			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				title = "An exception occurred when retrieving an list of the applications from NewRelic.";
			}

			yield return incomingMessage.ReplyToChannel(title, applicationAttachments);
		}

		private IEnumerable<ResponseMessage> UnhealthyApplicationsDetailHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			yield return incomingMessage.IndicateTypingOnChannel();

			string title;
			var applicationAttachments = new List<Attachment>();

			try
			{
				applicationAttachments = this.newRelicPlugin.GetUnhealthyApplicationsDetail();

				title = applicationAttachments.Any()
							? $"There are {applicationAttachments.Count} unhealthy applications in NewRelic. Those are:"
							: $"There are no unhealthy applications in NewRelic.";
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				title = "An exception occurred when retrieving an list of the applications from NewRelic.";
			}

			yield return incomingMessage.ReplyToChannel(title, applicationAttachments);
		}

		private IEnumerable<ResponseMessage> ApplicationDetailHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			yield return incomingMessage.IndicateTypingOnChannel();

			if (NewRelicPlugin.ApplicationDetailTargetedCommandMisformed(incomingMessage.TargetedText))
			{
				yield return incomingMessage.ReplyToChannel($"NewRelic application detail command was not formatted correctly. Help: {this.applicationDetailHelpText}");
				yield break;
			}

			var applicationName = this.newRelicPlugin.GetApplicationNameFromTargetedText(incomingMessage.TargetedText);
			var applicationAttachments = this.newRelicPlugin.GetApplicationDetailsFiltered(applicationName);

			if (!applicationAttachments.Any())
			{
				yield return incomingMessage.ReplyToChannel(
					$"No applications were found in NewRelic with the name `{applicationName}`");
			}
			else
			{
				yield return incomingMessage.ReplyToChannel(
					$"There were {applicationAttachments.Count} application(s) found for that name. They are:",
					applicationAttachments);
			}
		}

		private IEnumerable<ResponseMessage> ApplicationMetricsHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			yield return incomingMessage.IndicateTypingOnChannel();
			yield return incomingMessage.ReplyToChannel($"Sorry this command isn't supported yet.");
			yield break;

			//if (NewRelicPlugin.ApplicationDetailTargetedCommandMisformed(incomingMessage.TargetedText))
			//{
			//	yield return incomingMessage.ReplyToChannel($"NewRelic application metrics command was not formatted correctly. Help: {this.applicationMetricsHelpText}");
			//	yield break;
			//}

			//var application = this.newRelicPlugin.GetApplicationFromTargetedText(incomingMessage.TargetedText);
			//if (application == null)
			//{
			//	yield return incomingMessage.ReplyToChannel($"No NewRelic application could be found with the ID specified. Help: {this.applicationMetricsHelpText}");
			//	yield break;
			//}

			//yield return incomingMessage.ReplyToChannel(this.newRelicPlugin.GetSummaryMetricsText(application.Id));
		}
	}
}
