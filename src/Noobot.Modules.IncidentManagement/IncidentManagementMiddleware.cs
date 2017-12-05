using System;
using System.Collections.Generic;
using Common.Logging;
using Noobot.Core.MessagingPipeline.Middleware;
using Noobot.Core.MessagingPipeline.Middleware.ValidHandles;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;
using Noobot.Modules.NewRelic;

namespace Noobot.Modules.IncidentManagement
{
	public class IncidentManagementMiddleware : MiddlewareBase
	{
		private readonly IncidentManagementPlugin incidentManagementPlugin;

		private readonly string newIncidentHelpText = $"`{Configuration.Prefix} new website is offline`";

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

			var incidentText =
				this.incidentManagementPlugin.GetIncidentText($"{Configuration.Prefix} new", incomingMessage.TargetedText);
			var newChannelName = this.incidentManagementPlugin.GetNewChannelName(incidentText);

			var channelCreationFailed = false;

			try
			{
				var client = this.incidentManagementPlugin.GetSlackClient();
				var channel = client.CreateChannel(newChannelName);
				var blah = channel.Result;
			}
			catch (Exception e)
			{
				channelCreationFailed = true;
			}

			if (channelCreationFailed)
			{
				yield return incomingMessage.ReplyToChannel($"Incident creation of new channel {newChannelName} failed. Channel has not been created.");
				yield break;
			}
			

			yield return incomingMessage.ReplyToChannel($"Channel <#{newChannelName}> has been created to track issue '{incidentText}' by <@{incomingMessage.Username}>. Please direct all communication about the issue to the incidents channel.");
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
