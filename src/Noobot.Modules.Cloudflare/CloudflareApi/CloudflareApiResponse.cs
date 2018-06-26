using System;

using Newtonsoft.Json;

namespace Noobot.Modules.Cloudflare.CloudflareApi
{
	public partial class CloudflareApiResponse
	{
		[JsonProperty("success")]
		public bool Success { get; set; }

		[JsonProperty("errors")]
		public object[] Errors { get; set; }

		[JsonProperty("messages")]
		public object[] Messages { get; set; }

		[JsonProperty("result")]
		public Result[] Result { get; set; }

		[JsonProperty("result_info")]
		public ResultInfo ResultInfo { get; set; }
	}

	public partial class CloudflareApiSingleResponse
	{
		[JsonProperty("result")]
		public Result Result { get; set; }

		[JsonProperty("success")]
		public bool Success { get; set; }

		[JsonProperty("errors")]
		public object[] Errors { get; set; }

		[JsonProperty("messages")]
		public object[] Messages { get; set; }
	}

	public partial class Result
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("development_mode")]
		public long DevelopmentMode { get; set; }

		[JsonProperty("original_name_servers")]
		public string[] OriginalNameServers { get; set; }

		[JsonProperty("original_registrar")]
		public string OriginalRegistrar { get; set; }

		[JsonProperty("original_dnshost")]
		public string OriginalDnshost { get; set; }

		[JsonProperty("created_on")]
		public DateTimeOffset CreatedOn { get; set; }

		[JsonProperty("modified_on")]
		public DateTimeOffset ModifiedOn { get; set; }

		[JsonProperty("owner")]
		public Owner Owner { get; set; }

		[JsonProperty("permissions")]
		public string[] Permissions { get; set; }

		[JsonProperty("plan")]
		public Plan Plan { get; set; }

		[JsonProperty("plan_pending")]
		public Plan PlanPending { get; set; }

		[JsonProperty("status")]
		public string Status { get; set; }

		[JsonProperty("paused")]
		public bool Paused { get; set; }

		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("name_servers")]
		public string[] NameServers { get; set; }
	}

	public partial class Owner
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("email")]
		public string Email { get; set; }

		[JsonProperty("owner_type")]
		public string OwnerType { get; set; }
	}

	public partial class Plan
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("price")]
		public long Price { get; set; }

		[JsonProperty("currency")]
		public string Currency { get; set; }

		[JsonProperty("frequency")]
		public string Frequency { get; set; }

		[JsonProperty("legacy_id")]
		public string LegacyId { get; set; }

		[JsonProperty("is_subscribed")]
		public bool IsSubscribed { get; set; }

		[JsonProperty("can_subscribe")]
		public bool CanSubscribe { get; set; }
	}

	public partial class ResultInfo
	{
		[JsonProperty("page")]
		public long Page { get; set; }

		[JsonProperty("per_page")]
		public long PerPage { get; set; }

		[JsonProperty("count")]
		public long Count { get; set; }

		[JsonProperty("total_count")]
		public long TotalCount { get; set; }
	}
}
