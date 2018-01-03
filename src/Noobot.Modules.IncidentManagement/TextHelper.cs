using Noobot.Modules.IncidentManagement.Models;

namespace Noobot.Modules.IncidentManagement
{
	internal static class TextHelper
	{
		internal static string GetIncidentText(string commandPrefix, string message)
		{
			return message.Replace(commandPrefix, string.Empty).Trim();
		}

		public static string GetNewIncidentTextForMainIncidentChannel(Incident incident)
		{
			return $"Declared Timestamp: { incident.DeclaredDateTimeUtc } UTC\n"
					+ $"Reported By: @{ incident.DeclaredBy }\n"
					+ $"Bound To Channel: #{ incident.ChannelName }\n"
					+ $"Description: { incident.Title }";
		}

		public static string GetNewIncidentTextForWarRoomChannel(Incident incident)
		{
			return $"Incident #{ incident.FriendlyId } regarding '{ incident.Title }' has been declared by @{ incident.DeclaredBy } and bound to this channel.\n"
					+ "Please run `incident resolve` once the incident has been mitigated, and remember to add people to the channel that might be able to help. Good luck!";
		}

		public static string GetResolvedIncidentTextWithoutIncidentId(Incident incident)
		{
			return $"Declared Timestamp: { incident.DeclaredDateTimeUtc } UTC\n"
					+ $"Resolved Timestamp: { incident.ResolvedDateTimeUtc } UTC\n"
					+ $"Resolved By: @{ incident.ResolvedBy }\n"
					+ $"Channel: #{ incident.ChannelName }\n"
					+ $"Description: { incident.Title }";
		}

		public static string GetClosedIncidentTextWithoutIncidentId(Incident incident)
		{
			return $"Declared Timestamp: { incident.DeclaredDateTimeUtc } UTC\n"
					+ $"Resolved Timestamp: { incident.ResolvedDateTimeUtc } UTC\n"
					+ $"Closed Timestamp: { incident.ClosedDateTimeUtc } UTC\n"
					+ $"Closed By: @{ incident.ClosedBy }\n"
					+ $"Channel: #{ incident.ChannelName }\n"
					+ $"Description: { incident.Title }";
		}
	}
}
