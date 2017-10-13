using System.Collections.Generic;

namespace Noobot.Modules.LoadBalancerDotOrg.Models
{
	public class ApiRequest
	{
		public ApiRequest(string apiKey, string command, string vip, string rip)
		{
			this.auth = new Auth { apikey = apiKey };
			this.action = new List<Action> { new Action { command = command } };
			this.syntax = new List<Syntax> { new Syntax { vip = vip, rip = rip } };
		}

		public Auth auth { get; }

		public List<Action> action { get; }

		public List<Syntax> syntax { get; }
	}
}