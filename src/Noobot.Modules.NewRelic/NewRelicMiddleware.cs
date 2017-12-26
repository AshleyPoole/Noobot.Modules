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

			string responseText;

			try
			{
				responseText = this.newRelicPlugin.GetApplicationsText();
				

			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				responseText = "An exception occurred when retrieving an list of the applications from NewRelic.";
			}

			yield return incomingMessage.ReplyToChannel(responseText);
		}

		private IEnumerable<ResponseMessage> ApplicationSummaryHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			incomingMessage.IndicateTypingOnChannel();

			if (NewRelicPlugin.ApplicationTargetedCommandMisformed(incomingMessage.TargetedText))
			{
				yield return incomingMessage.ReplyToChannel($"NewRelic application summary was not formatted correctly. Help: {this.applicationSummaryHelpText}");
				yield break;
			}

			var application = this.newRelicPlugin.GetApplicationFromTargetedText(incomingMessage.TargetedText);
			if (application == null)
			{
				yield return incomingMessage.ReplyToChannel($"No NewRelic application could be found with the ID specified. Help: {this.applicationSummaryHelpText}");
				yield break;
			}

			yield return incomingMessage.ReplyToChannel(this.newRelicPlugin.GetApplicationHealthText(application));
		}

		private IEnumerable<ResponseMessage> AllApplicationSummaryHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			incomingMessage.IndicateTypingOnChannel();

			yield return incomingMessage.ReplyToChannel(this.newRelicPlugin.GetApplicationsSummaryText());
		}

		private IEnumerable<ResponseMessage> ApplicationMetricsHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			incomingMessage.IndicateTypingOnChannel();
			yield return incomingMessage.ReplyToChannel($"Sorry this command isn't supported yet.");
			yield break;

			if (NewRelicPlugin.ApplicationTargetedCommandMisformed(incomingMessage.TargetedText))
			{
				yield return incomingMessage.ReplyToChannel($"NewRelic application metrics command was not formatted correctly. Help: {this.applicationMetricsHelpText}");
				yield break;
			}

			var application = this.newRelicPlugin.GetApplicationFromTargetedText(incomingMessage.TargetedText);
			if (application == null)
			{
				yield return incomingMessage.ReplyToChannel($"No NewRelic application could be found with the ID specified. Help: {this.applicationMetricsHelpText}");
				yield break;
			}

			yield return incomingMessage.ReplyToChannel(this.newRelicPlugin.GetSummaryMetricsText(application.Id));
		}
	}
}
