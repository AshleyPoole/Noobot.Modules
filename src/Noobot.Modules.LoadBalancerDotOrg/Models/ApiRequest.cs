using System.Collections.Generic;
using Newtonsoft.Json;

namespace Noobot.Modules.LoadBalancerDotOrg.Models
{
	internal class ApiRequest
	{
		internal ApiRequest(string apiKey, string command, string vip, string rip)
		{
			this.Auth = new Auth { Apikey = apiKey };
			this.Action = new List<Action> { new Action { Command = command } };
			this.Syntax = new List<Syntax> { new Syntax { Vip = vip, Rip = rip } };
		}

		[JsonProperty("atuh")]
		public Auth Auth { get; }

		[JsonProperty("action")]
		public List<Action> Action { get; }

		[JsonProperty("syntax")]
		public List<Syntax> Syntax { get; }
	}
}