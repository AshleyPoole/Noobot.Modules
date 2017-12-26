using System;
using System.Collections.Generic;
using System.Net.Http;

using Common.Logging;

using Noobot.Core.MessagingPipeline.Middleware;
using Noobot.Core.MessagingPipeline.Middleware.ValidHandles;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;
using Noobot.Modules.LoadBalancerDotOrg.Models;

namespace Noobot.Modules.LoadBalancerDotOrg
{
	public class LboMiddleware : MiddlewareBase
	{
		private readonly ILog log;
		private readonly LboPlugin lboPlugin;

		public LboMiddleware(IMiddleware next, LboPlugin lboPlugin, ILog log) : base(next)
		{
			this.log = log;
			this.lboPlugin = lboPlugin;

			this.HandlerMappings = new[]
			{
				new HandlerMapping
				{
					ValidHandles = RegexHandle.For($"^{Configuration.Prefix} [^ ]+ drain.+"),
					EvaluatorFunc = this.DrainHandler,
					Description = $"Drains a node on the load balancer. '{GetHelpText(LoadBalancerActions.Drain)}'",
					VisibleInHelp = false
				},
				new HandlerMapping
				{
					ValidHandles = RegexHandle.For($"^{Configuration.Prefix} [^ ]+ halt.+"),
					EvaluatorFunc = this.HaltHandler,
					Description = $"Halts a node on the load balancer. '{GetHelpText(LoadBalancerActions.Halt)}'",
					VisibleInHelp = false
				},
				new HandlerMapping
				{
					ValidHandles = RegexHandle.For($"^{Configuration.Prefix} [^ ]+ online.+"),
					EvaluatorFunc = this.OnlineHandler,
					Description = $"Brings a node online on the load balancer. '{GetHelpText(LoadBalancerActions.Online)}'",
					VisibleInHelp = false
				},
			};
		}

		private IEnumerable<ResponseMessage> DrainHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			return this.GenericCommandHandler(incomingMessage, LoadBalancerActions.Drain);
		}

		private IEnumerable<ResponseMessage> HaltHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			return this.GenericCommandHandler(incomingMessage, LoadBalancerActions.Halt);
		}

		private IEnumerable<ResponseMessage> OnlineHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			return this.GenericCommandHandler(incomingMessage, LoadBalancerActions.Online);
		}

		private IEnumerable<ResponseMessage> GenericCommandHandler(IncomingMessage incomingMessage, string command)
		{
			Exception exception = null;
			HttpResponseMessage apiResponseMessage = null;

			incomingMessage.IndicateTypingOnChannel();

			if (this.lboPlugin.CommandMisformed(incomingMessage.TargetedText))
			{
				yield return incomingMessage.ReplyToChannel($"Command was not formatted correctly. Help: '{GetHelpText(command)}'");
				yield break;
			}

			var lboRequest = new LoadBalancerRequest(incomingMessage.TargetedText);

			var appliance = this.lboPlugin.GetAppliance(lboRequest.ApplianceName);
			if (appliance == null)
			{
				yield return incomingMessage.ReplyToChannel($"No appliance could be found with the name:{lboRequest.ApplianceName}. Help: '{GetHelpText(command)}'");
				yield break;
			}

			try
			{
				apiResponseMessage = this.lboPlugin.MakeApiRequest(appliance, lboRequest);
			}
			catch (Exception e)
			{
				this.log.Error(e);
				exception = e;
			}

			if (this.lboPlugin.ApiRequestThrewException(apiResponseMessage, exception))
			{
				yield return incomingMessage.ReplyToChannel($"An exception occured when communicating to applicance '{appliance.Name} ({appliance.ApiUrl}).");
				yield break;
			}

			var lbResponse = this.lboPlugin.ParseApiResponse(apiResponseMessage);

			if (lbResponse.Contains("Error"))
			{
				yield return incomingMessage.ReplyToChannel($"An error occured when issuing command against VIP {lboRequest.Vip} for RIP {lboRequest.Rip}. Check the VIP and RIP names are valided for applicance `{appliance.Name}` ({appliance.ApiUrl}).");
				yield break;
			}

			yield return incomingMessage.ReplyToChannel($"{lbResponse}");
		}

		private static string GetHelpText(string command)
		{
			return $"`{Configuration.Prefix} device01 ||action|| webserver01 virtualservice01`".Replace("||action||", command);
		}
	}
}
