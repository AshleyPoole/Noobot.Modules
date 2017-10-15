using System;

namespace Noobot.Modules.LoadBalancerDotOrg.Models
{
	internal class LoadBalancerRequest
	{
		internal LoadBalancerRequest(string message)
		{
			var messageParts = message.Split(" ", StringSplitOptions.RemoveEmptyEntries);

			this.ApplianceName = messageParts[1];
			this.Command = messageParts[2];
			this.Vip = messageParts[3];
			this.Rip = messageParts[4];
		}

		public string ApplianceName { get; }

		public string Command { get; }

		public string Vip { get; }

		public string Rip { get; }
	}
}