using System;
using System.Collections.Generic;
using Common.Logging;
using Noobot.Core.MessagingPipeline.Middleware;
using Noobot.Core.MessagingPipeline.Middleware.ValidHandles;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;

namespace Noobot.Modules.Dns
{
	public class DnsMiddleware : MiddlewareBase
	{
		private const string Lookup = "lookup";

		private readonly DnsPlugin dnsPlugin;

		private readonly ILog log;

		public DnsMiddleware(IMiddleware next, DnsPlugin dnsPlugin, ILog log)
			: base(next)
		{
			this.dnsPlugin = dnsPlugin;
			this.log = log;
			this.HandlerMappings = new[]
									{
										new HandlerMapping
										{
											ValidHandles = StartsWithHandle.For($"{Configuration.CommandPrefix} {Lookup}"),
											EvaluatorFunc = this.DnsLookupHandler,
											Description = $"Gets IP address for dns entry. {GetHelpText(Lookup)}",
											VisibleInHelp = true
										},
									};
		}

		private IEnumerable<ResponseMessage> DnsLookupHandler(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			incomingMessage.IndicateTypingOnChannel();

			if (!DnsLookupCommandWellFormatted(incomingMessage.TargetedText))
			{
				yield return incomingMessage.ReplyToChannel($"Command was not formatted correctly. Help: {GetHelpText(Lookup)}");
				yield break;
			}

			var hostToLookup = this.dnsPlugin.GetHostFromMessage(incomingMessage.TargetedText);
			var ipAddresses = this.dnsPlugin.LookUpIpFromCommandText(incomingMessage.TargetedText);

			if (ipAddresses == null)
			{
				yield return incomingMessage.ReplyToChannel($"Error looking up requested host '{hostToLookup}'. No result were returned.");
				yield break;
			}
			
			yield return incomingMessage.ReplyToChannel($"dns lookup for '{hostToLookup}' returned: {this.dnsPlugin.GetIpAddressesAsText(ipAddresses)}");
		}

		private static bool DnsLookupCommandWellFormatted(string message)
		{
			return message.Split(" ", StringSplitOptions.RemoveEmptyEntries).Length == 3;
		}

		private static string GetHelpText(string command)
		{
			return $"`{Configuration.CommandPrefix} ||action|| www.ashleypoole.co.uk`".Replace("||action||", command);
		}
	}
}
