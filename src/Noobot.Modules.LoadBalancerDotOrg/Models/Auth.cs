using Newtonsoft.Json;

namespace Noobot.Modules.LoadBalancerDotOrg.Models
{
	internal class Auth
	{
		[JsonProperty("apiKey")]
		public string Apikey { get; set; }
	}
}