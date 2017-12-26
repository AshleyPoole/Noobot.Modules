using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

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

		internal IPAddress[] LookUpIpFromCommandText(string hostToLookup)
		{
			
			IPAddress[] ipAddresses;

			try
			{
				ipAddresses = System.Net.Dns.GetHostAddresses(hostToLookup);
			}
			catch (Exception)
			{
				ipAddresses = null;
			}

			return ipAddresses;
		}

		internal string GetIpAddressesAsText(IEnumerable<IPAddress> ipAddresses)
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

		internal string GetHostFromMessage(string messageText)
		{
			var hostText = messageText.Split(" ", StringSplitOptions.RemoveEmptyEntries)[2];
			return hostText.Contains("|") ? hostText.Substring(hostText.IndexOf("|", StringComparison.Ordinal) + 1).Replace(">", string.Empty) : messageText;
		}
	}
}
