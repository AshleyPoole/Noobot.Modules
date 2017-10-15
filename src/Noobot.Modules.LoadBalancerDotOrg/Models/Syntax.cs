using Newtonsoft.Json;

namespace Noobot.Modules.LoadBalancerDotOrg.Models
{
	internal class Syntax
	{
		[JsonProperty("vip")]
		public string Vip { get; set; }

		[JsonProperty("rip")]
		public string Rip { get; set; }
	}
}