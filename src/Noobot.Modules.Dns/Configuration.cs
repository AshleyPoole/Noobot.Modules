using Noobot.Core.Configuration;

namespace Noobot.Modules.Dns
{
	public class Configuration : ConfigurationBase
	{
		internal const string CommandPrefix = "dns";

		public Configuration()
		{
			this.UseMiddleware<DnsMiddleware>();
		}
	}
}
