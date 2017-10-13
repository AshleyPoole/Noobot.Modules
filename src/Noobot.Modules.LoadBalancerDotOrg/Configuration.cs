using Noobot.Core.Configuration;

namespace Noobot.Modules.LoadBalancerDotOrg
{
	public class Configuration : ConfigurationBase
	{
		public const string Prefix = "lbo";

		public Configuration()
		{
			this.UseMiddleware<LboMiddleware>();
			this.UsePlugin<LboPlugin>();
		}
	}
}
