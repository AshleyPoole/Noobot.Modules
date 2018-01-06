using System;
using System.Collections.Generic;
using System.Net;

using Noobot.Core.Plugins;

namespace Noobot.Modules.Dns
{
	public class DnsPlugin : IPlugin
	{
		public void Start()
		{
		}

		public void Stop()
		{
		}

		internal static IPAddress[] LookUpIpFromCommandText(string hostToLookup)
		{
			
			IPAddress[] ipAddresses;

			try
			{
				ipAddresses = System.Net.Dns.GetHostAddresses(hostToLookup);
			}
			catch (Exception ex)
			{
				ipAddresses = null;
			}

			return ipAddresses;
		}

		internal static string GetIpAddressesAsText(IEnumerable<IPAddress> ipAddresses)
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

		internal static string GetHostFromMessage(string messageText)
		{
			var hostText = messageText.Split(" ", StringSplitOptions.RemoveEmptyEntries)[2];
			return hostText.Contains("|") ? hostText.Substring(hostText.IndexOf("|", StringComparison.Ordinal) + 1).Replace(">", string.Empty) : messageText;
		}
	}
}
