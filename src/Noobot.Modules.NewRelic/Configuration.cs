using Noobot.Core.Configuration;

namespace Noobot.Modules.NewRelic
{
	public class Configuration : ConfigurationBase
	{
		public const string CommandPrefix = "newrelic";

		public Configuration()
		{
			this.UseMiddleware<NewRelicMiddleware>();
			this.UsePlugin<NewRelicPlugin>();
		}
	}
}
