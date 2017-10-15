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
			return this.GenericCommandHandler(incomingMessage, LoadBalancerActions.Drain);
		}

		private IEnumerable<ResponseMessage> OnlineHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			return this.GenericCommandHandler(incomingMessage, LoadBalancerActions.Drain);
		}

		private IEnumerable<ResponseMessage> GenericCommandHandler(IncomingMessage incomingMessage, string command)
		{
			Exception exception = null;
			HttpResponseMessage httpResponse = null;

			incomingMessage.IndicateTypingOnChannel();

			if (!LboPlugin.CommandWellFormatted(incomingMessage.TargetedText))
			{
				yield return incomingMessage.ReplyToChannel($"Command was not formatted correctly. Help: '{GetHelpText(command)}'");
				yield break;
			}

			var lboRequest = new LoadBalancerRequest(incomingMessage.TargetedText);

			var appliance = this.lboPlugin.GetAppliance(lboRequest.ApplianceName);
			if (appliance == null)
			{
				yield return incomingMessage.ReplyToChannel($"No applaince could be found with the name {lboRequest.ApplianceName}. Help: '{GetHelpText(command)}'");
				yield break;
			}

			using (var handler = new HttpClientHandler())
			{
				if (this.lboPlugin.TrustAllCerts)
				{
					handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
				}

				using (var client = new HttpClient(handler))
				{
					try
					{
						
						client.DefaultRequestHeaders.Authorization = this.lboPlugin.GetAuthHeader(appliance);
						httpResponse = client.PostAsync(appliance.ApiUrl, this.lboPlugin.GetRequestContent(appliance, lboRequest)).Result;
					}
					catch (Exception e)
					{
						this.log.Error(e);
						exception = e;
					}
				}
			}

			if (exception != null || httpResponse == null || !httpResponse.IsSuccessStatusCode)
			{
				yield return incomingMessage.ReplyToChannel($"An exception occured when communicating to applicance '{appliance.Name} ({appliance.ApiUrl}).");
				yield break;
			}

			var lbResponse = httpResponse.Content.ReadAsStringAsync().Result;
			lbResponse = lbResponse.Replace("\n", string.Empty).Replace("LBCLI:", string.Empty);

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
