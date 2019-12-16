using Noobot.Core.Configuration;

namespace Noobot.Modules.LoadBalancerDotOrg
{
	public class Configuration : ConfigurationBase
	{
		internal const string Prefix = "lbo";

		public Configuration()
		{
			this.UseMiddleware<LboMiddleware>();
			this.UsePlugin<LboPlugin>();
		}
	}
}
