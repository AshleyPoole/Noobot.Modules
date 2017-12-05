using Noobot.Core.Configuration;
using Noobot.Modules.NewRelic;

namespace Noobot.Modules.IncidentManagement
{
	public class Configuration : ConfigurationBase
	{
		public const string Prefix = "incident";

		public Configuration()
		{
			this.UseMiddleware<IncidentManagementMiddleware>();
			this.UsePlugin<IncidentManagementPlugin>();
		}
	}
}
