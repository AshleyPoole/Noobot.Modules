using Noobot.Core.Configuration;

namespace Noobot.Modules.NewRelic
{
	public class Configuration : ConfigurationBase
	{
		internal const string CommandPrefix = "newrelic";

		internal const string NewRelicGoodStatus = "green";

		internal const string NewRelicGoodStatusColor = "good";

		internal const string NewRelicWarningStatus = "orange";

		internal const string NewRelicWarningStatusColor = "warning";

		internal const string NewRelicBadStatus = "red";

		internal const string NewRelicBadStatusColor = "danger";

		internal const string NewRelicUnknownStatusColor = "#439FE0";

		public void RegisterModule()
		{
			this.UseMiddleware<NewRelicMiddleware>();
			this.UsePlugin<NewRelicPlugin>();
		}
	}
}
