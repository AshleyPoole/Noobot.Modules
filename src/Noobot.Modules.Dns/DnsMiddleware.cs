
using System;
using System.Collections.Generic;
using System.Net;
using Noobot.Core.MessagingPipeline.Middleware;
using Noobot.Core.MessagingPipeline.Middleware.ValidHandles;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;

namespace Noobot.Modules.Dns
{
	public class DnsMiddleware : MiddlewareBase
	{
		private const string Lookup = "lookup";

		public DnsMiddleware(IMiddleware next)
			: base(next)
		{
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

			var hostToLookup = GetHostFromMessage(incomingMessage.TargetedText);
			var ipAddresses = new IPAddress[] { };
			var errorDuringLookup = false;
			
			try
			{
				ipAddresses = System.Net.Dns.GetHostAddresses(hostToLookup);
			}
			catch (Exception e)
			{
				errorDuringLookup = true;
				Console.WriteLine(e);
			}

			if (errorDuringLookup || ipAddresses == null)
			{
				yield return incomingMessage.ReplyToChannel($"Error looking up requested host '{hostToLookup}'. No result were returned.");
				yield break;
			}
			
			yield return incomingMessage.ReplyToChannel($"dns lookup for '{hostToLookup}' returned: {GetIpAddressesAsText(ipAddresses)}");
		}

		private static bool DnsLookupCommandWellFormatted(string message)
		{
			return message.Split(" ", StringSplitOptions.RemoveEmptyEntries).Length == 3;
		}

		private static string GetHelpText(string command)
		{
			return $"`{Configuration.CommandPrefix} ||action|| www.ashleypoole.co.uk`".Replace("||action||", command);
		}

		private static string GetHostFromMessage(string messageText)
		{
			var hostText = messageText.Split(" ", StringSplitOptions.RemoveEmptyEntries)[2];
			return hostText.Contains("|") ? hostText.Substring(hostText.IndexOf("|", StringComparison.Ordinal) + 1).Replace(">", String.Empty) : messageText;
		}

		public static string GetIpAddressesAsText(IPAddress[] ipAddresses)
		{
			var ipAddressesMessage = string.Empty;

			foreach (var ip in ipAddresses)
			{
				if (!string.IsNullOrWhiteSpace(ipAddressesMessage))
				{
					ipAddressesMessage += ", ";
				}

				ipAddressesMessage += ip.ToString();
			}

			return ipAddressesMessage;
		}
	}
}
