using System.Collections.Generic;
using Common.Logging;
using Noobot.Core.MessagingPipeline.Middleware;
using Noobot.Core.MessagingPipeline.Middleware.ValidHandles;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;

namespace Noobot.Modules.IncidentManagement
{
	public class IncidentManagementMiddleware : MiddlewareBase
	{
		private readonly IncidentManagementPlugin incidentManagementPlugin;

		private readonly string newIncidentHelpText = $"`{Configuration.Prefix} new`";

		private readonly string resolveIncidentHelpText = $"`{Configuration.Prefix} resolve`";

		private readonly string closeIncidentHelpText = $"`{Configuration.Prefix} close`";

		public IncidentManagementMiddleware(IMiddleware next, IncidentManagementPlugin incidentManagementPlugin, ILog log)
			: base(next)
		{
			this.incidentManagementPlugin = incidentManagementPlugin;
			this.HandlerMappings = new[]
									{
										new HandlerMapping
										{
											ValidHandles = StartsWithHandle.For($"{Configuration.Prefix} new"),
											EvaluatorFunc = this.NewIncidentHandler,
											Description = $"Declares a new incident and creates a dedicated. '{this.newIncidentHelpText}'",
											VisibleInHelp = true
										},
										new HandlerMapping
										{
											ValidHandles = ExactMatchHandle.For($"{Configuration.Prefix} resolve"),
											EvaluatorFunc = this.ResolveIncidentHandler,
											Description = $"Resolve the incident associated with current channel. {this.resolveIncidentHelpText}",
											VisibleInHelp = true
										},
										new HandlerMapping
										{
											ValidHandles = ExactMatchHandle.For($"{Configuration.Prefix} close"),
											EvaluatorFunc = this.CloseIncidentHandler,
											Description = $"Close the incident associated with current channel. {this.closeIncidentHelpText}",
											VisibleInHelp = true
										},
									};
		}

		private IEnumerable<ResponseMessage> NewIncidentHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			incomingMessage.IndicateTypingOnChannel();

			if (!this.incidentManagementPlugin.CommandWellFormatted(incomingMessage.TargetedText))
			{
				yield return incomingMessage.ReplyToChannel($"Please provide incident title. Help: {this.newIncidentHelpText}");
			}

			if (incomingMessage.Channel == this.incidentManagementPlugin.MainIncidentChannel)
			{
				yield return incomingMessage.ReplyToChannel($"Sorry, new incidents cannot be declared in this channel as bots are unable to create new channels. Please create a dedicated channel for this incident and declare the incident from there.");
			}

			if (incomingMessage.ChannelType == ResponseType.DirectMessage)
			{
				yield return incomingMessage.ReplyToChannel($"Sorry, new incidents cannot be declared in direct messages. Create a public channel in order to declare incidents... We all want to take part!");
			}

			var incidentText =
				this.incidentManagementPlugin.GetIncidentText($"{Configuration.Prefix} new", incomingMessage.TargetedText);

			this.incidentManagementPlugin.SendNewIncidentCreatedMessage(incidentText, incomingMessage.Username, incomingMessage.Channel);

			yield return incomingMessage.ReplyToChannel($"New incident succesfully declared and posted to #{this.incidentManagementPlugin.MainIncidentChannel}... "
														+ $"Remember to add people to this channel that may be able to help mitigate this incident... Good luck!");
		}

		private IEnumerable<ResponseMessage> ResolveIncidentHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			incomingMessage.IndicateTypingOnChannel();

			yield return incomingMessage.ReplyToChannel($"Not impelemented.");
		}

		private IEnumerable<ResponseMessage> CloseIncidentHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			incomingMessage.IndicateTypingOnChannel();

			yield return incomingMessage.ReplyToChannel($"Not implement.");
		}

	}
}
