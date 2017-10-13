using Noobot.Core.Configuration;

namespace Noobot.Modules.Dns
{
	public class Configuration : ConfigurationBase
	{
		public const string CommandPrefix = "dns";

		public Configuration()
		{
			this.UseMiddleware<DnsMiddleware>();
		}
	}
}
