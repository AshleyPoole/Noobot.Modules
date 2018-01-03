using System.Collections.Generic;
using System.Linq;

using Common.Logging;
using Noobot.Core.MessagingPipeline.Middleware;
using Noobot.Core.MessagingPipeline.Middleware.ValidHandles;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;
using Noobot.Modules.IncidentManagement.Models;

namespace Noobot.Modules.IncidentManagement
{
	public class IncidentManagementMiddleware : MiddlewareBase
	{
		private readonly IncidentManagementPlugin incidentManagementPlugin;

		private readonly string newIncidentHelpText = $"`{Configuration.Prefix} new`";

		private readonly string resolveIncidentHelpText = $"`{Configuration.Prefix} resolve`";

		private readonly string closeIncidentHelpText = $"`{Configuration.Prefix} close`";

		private readonly string listActiveIncidentHelpText = $"`{Configuration.Prefix} list active`";

		private readonly string listRecentIncidentHelpText = $"`{Configuration.Prefix} list recent`";

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
											Description = $"Declares a new incident. {this.newIncidentHelpText}",
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
										new HandlerMapping
										{
											ValidHandles = ExactMatchHandle.For($"{Configuration.Prefix} list active"),
											EvaluatorFunc = this.ListActiveIncidentHandler,
											Description = $"Lists active incidents. {this.listActiveIncidentHelpText}",
											VisibleInHelp = true
										},
										new HandlerMapping
										{
											ValidHandles = ExactMatchHandle.For($"{Configuration.Prefix} list recent"),
											EvaluatorFunc = this.ListRecentIncidentHandler,
											Description = $"Lists recent incidents. {this.listRecentIncidentHelpText}",
											VisibleInHelp = true
										},
									};
		}

		private IEnumerable<ResponseMessage> NewIncidentHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			yield return incomingMessage.IndicateTypingOnChannel();

			if (!this.incidentManagementPlugin.NewIncidentCommandWellFormatted(incomingMessage.TargetedText))
			{
				yield return incomingMessage.ReplyToChannel($"Please provide incident title. Help: {this.newIncidentHelpText}");
			}

			var channel = new Channel(
				incomingMessage.Channel,
				this.incidentManagementPlugin.GetUserFriendlyChannelName(incomingMessage.Channel));

			if (channel.Name == this.incidentManagementPlugin.MainIncidentChannel)
			{
				yield return incomingMessage.ReplyToChannel($"Sorry, new incidents cannot be declared in this channel as bots are unable to create new channels. Please create a dedicated channel for this incident and declare the incident from there.");
			}

			if (incomingMessage.ChannelType == ResponseType.DirectMessage)
			{
				yield return incomingMessage.ReplyToChannel($"Sorry, new incidents cannot be declared in direct messages. Create a public channel in order to declare incidents... We all want to take part!");
			}

			var incidentText =
				this.incidentManagementPlugin.GetIncidentText($"{Configuration.Prefix} new", incomingMessage.TargetedText);

			var incident = this.incidentManagementPlugin.DeclareNewIncident(incidentText, incomingMessage.Username, channel);

			yield return incomingMessage.ReplyToChannel($"Incident #{incident.FriendlyId} succesfully declared and posted to #{ this.incidentManagementPlugin.MainIncidentChannel }.\n"
														+ $"Remember to add people to this channel that may be able to help mitigate this incident. Run { this.resolveIncidentHelpText } after the incident is mitigated. \n"
														+ $"Good luck!");
		}

		private IEnumerable<ResponseMessage> ResolveIncidentHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			yield return incomingMessage.IndicateTypingOnChannel();

			var friendlyChannelName = this.incidentManagementPlugin.GetUserFriendlyChannelName(incomingMessage.Channel);
			var incident = this.incidentManagementPlugin.ResolveIncident(incomingMessage.Username, friendlyChannelName);

			if (incident == null)
			{
				yield return incomingMessage.ReplyToChannel(
					"Sorry, no unresolved incident was found attached to this channel. Was the incident already resolved or closed?");
			}
			else
			{
				yield return incomingMessage.ReplyToChannel($"Incident #{ incident.FriendlyId } succesfully resolved. Please run { this.closeIncidentHelpText } once the incident has been stood down.");
			}
		}

		private IEnumerable<ResponseMessage> CloseIncidentHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			yield return incomingMessage.IndicateTypingOnChannel();

			var friendlyChannelName = this.incidentManagementPlugin.GetUserFriendlyChannelName(incomingMessage.Channel);
			var incident = this.incidentManagementPlugin.CloseIncident(incomingMessage.Username, friendlyChannelName);

			if (incident == null)
			{
				yield return incomingMessage.ReplyToChannel(
					$"Sorry, no incident was found attached to this channel that could be closed. Was the incident resolved yet or already closed? "
					+ $"If it the incident hasn't been marked as resolved, run { this.resolveIncidentHelpText } first.");
			}
			else
			{
				yield return incomingMessage.ReplyToChannel($"Incident #{ incident.FriendlyId } succesfully closed. You can now archive the channel and prepare the postmortem on Jive.");

			}
		}

		private IEnumerable<ResponseMessage> ListActiveIncidentHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			yield return incomingMessage.IndicateTypingOnChannel();

			var openIncidentAttachments = this.incidentManagementPlugin.GetOpenIncidents();

			if (openIncidentAttachments.Any())
			{
				yield return incomingMessage.ReplyToChannel(
					$"There are {openIncidentAttachments.Count} incident(s) currently open:",
					openIncidentAttachments);
			}
			else
			{
				yield return incomingMessage.ReplyToChannel(
					$"Great news! There's no active incidents at the moment. If you need to declare a new incident, run { this.newIncidentHelpText }.");

			}
		}

		private IEnumerable<ResponseMessage> ListRecentIncidentHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			yield return incomingMessage.IndicateTypingOnChannel();

			var recentIncidentAttachments = this.incidentManagementPlugin.GetRecentIncidents();

			yield return incomingMessage.ReplyToChannel($"There were { recentIncidentAttachments.Count } recent incident(s):", recentIncidentAttachments);
		}
	}
}
