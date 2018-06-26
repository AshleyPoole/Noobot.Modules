using System;
using System.Collections.Generic;

using Common.Logging;

using Noobot.Core.MessagingPipeline.Middleware;
using Noobot.Core.MessagingPipeline.Middleware.ValidHandles;
using Noobot.Core.MessagingPipeline.Request;
using Noobot.Core.MessagingPipeline.Response;

namespace Noobot.Modules.Cloudflare
{
	public class CloudflareMiddleware : MiddlewareBase
	{
		private readonly CloudflarePlugin cloudflarePlugin;

		private readonly ILog log;

		private const string Purge = "purge";

		public CloudflareMiddleware(IMiddleware next, CloudflarePlugin cloudflarePlugin, ILog log)
			: base(next)
		{
			this.cloudflarePlugin = cloudflarePlugin;
			this.log = log;
			this.HandlerMappings = new[]
									{
										new HandlerMapping
										{
											ValidHandles = StartsWithHandle.For($"{Configuration.CommandPrefix} {Purge} tag"),
											EvaluatorFunc = this.PurgeCacheTag,
											Description = $"Purges Cloudflare cache for specified cache tag. {GetPurgeCacheTagHelpText()}",
											VisibleInHelp = true
										},
										new HandlerMapping
										{
											ValidHandles = StartsWithHandle.For($"{Configuration.CommandPrefix} {Purge} zone"),
											EvaluatorFunc = this.PurgeZone,
											Description = $"Purges Cloudflare cache for specified zone. { GetPurgeZoneHelpText()}",
											VisibleInHelp = true
										}
									};
		}

		private IEnumerable<ResponseMessage> PurgeZone(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			yield return incomingMessage.IndicateTypingOnChannel();

			if (!CommandWellFormatted(incomingMessage.TargetedText, requiredCommandLength: 4))
			{
				yield return incomingMessage.ReplyToChannel($"Command was not formatted correctly. Help: {GetPurgeZoneHelpText()}");
				yield break;
			}

			var zoneName = GetCleanZoneName(GetPositionalElementFromTargetText(incomingMessage.TargetedText, position: 3));
			var result = this.cloudflarePlugin.PurgeZone(zoneName);

			yield return incomingMessage.ReplyToChannel(result.Message);
		}

		private IEnumerable<ResponseMessage> PurgeCacheTag(IncomingMessage incomingMessage, IValidHandle matchedHandle)
		{
			yield return incomingMessage.IndicateTypingOnChannel();

			if (!CommandWellFormatted(incomingMessage.TargetedText, requiredCommandLength: 5))
			{
				yield return incomingMessage.ReplyToChannel($"Command was not formatted correctly. Help: {GetPurgeCacheTagHelpText()}");
				yield break;
			}

			var cacheTag = GetPositionalElementFromTargetText(incomingMessage.TargetedText, position: 3);
			var zoneName = GetCleanZoneName(GetPositionalElementFromTargetText(incomingMessage.TargetedText, position:4));

			var result = this.cloudflarePlugin.PurgeZoneCacheTag(zoneName, cacheTag);

			yield return incomingMessage.ReplyToChannel(result.Message);
		}

		private static bool CommandWellFormatted(string message, int requiredCommandLength)
		{
			return message.Split(" ", StringSplitOptions.RemoveEmptyEntries).Length == requiredCommandLength;
		}

		private static string GetPurgeCacheTagHelpText()
		{
			return $"`{Configuration.CommandPrefix} purge tag PROD-MyApp ashleypoole.co.uk`";
		}

		private static string GetPurgeZoneHelpText()
		{
			return $"`{Configuration.CommandPrefix} purge zone ashleypoole.co.uk`";
		}

		private static string GetPositionalElementFromTargetText(string messageText, int position)
		{
			return messageText.Split(" ", StringSplitOptions.RemoveEmptyEntries)[position];
		}

		private static string GetCleanZoneName(string zoneName)
		{
			return zoneName.Contains("|") ? zoneName.Substring(zoneName.IndexOf("|", StringComparison.Ordinal) + 1).Replace(">", string.Empty) : zoneName;
		}
	}
}
