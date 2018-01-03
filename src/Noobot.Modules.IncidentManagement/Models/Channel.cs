namespace Noobot.Modules.IncidentManagement.Models
{
	internal class Channel
	{
		public Channel(string channelId, string channelName)
		{
			this.Id = channelId;
			this.Name = channelName;
		}

		public readonly string Id;

		public readonly string Name;
	}
}
