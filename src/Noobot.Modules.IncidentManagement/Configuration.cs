using Noobot.Core.Configuration;

using StructureMap.Building;

namespace Noobot.Modules.IncidentManagement
{
	public class Configuration : ConfigurationBase
	{
		public const string Prefix = "incident";

		public const string UnresolvedIncidentColor = "danger";

		public const string ResolvedIncidentColor = "good";

		public const string ClosedIncidentColor = "#439FE0";
	}
}
