using Newtonsoft.Json;

namespace Noobot.Modules.LoadBalancerDotOrg.Models
{
	internal class Action
	{
		[JsonProperty("command")]
		public string Command { get; set; }
	}
}