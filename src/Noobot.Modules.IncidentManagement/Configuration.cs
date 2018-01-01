using Noobot.Core.Configuration;

namespace Noobot.Modules.IncidentManagement
{
	internal class Configuration : ConfigurationBase
	{
		public const string Prefix = "incident";

		public const string UnresolvedIncidentColor = "danger";

		public const string ResolvedIncidentColor = "good";

		public const string ClosedIncidentColor = "#439FE0";
	}
}
