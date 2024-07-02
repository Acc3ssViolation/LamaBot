using LamaBot.Quotes;
using LamaBot.Servers;
using Microsoft.AspNetCore.Mvc;

namespace LamaBot.Forum
{
    [ApiController]
    [Route("api/v1/forum")]
    public class ForumApiController : ControllerBase
    {
        public record ForumChannel(ulong Id, string Name, DateTimeOffset CreatedAt, List<ForumThread> Threads);

        public record ForumThread(ulong Id, string Name, DateTimeOffset CreatedAt, bool Archived);

        [HttpGet("{guildId}")]
        public async Task<IActionResult> GetForumThreadsAsync([FromServices] IDiscordFacade facade, [FromServices] IServerSettings serverSettings, [FromRoute] ulong guildId, [FromQuery] string key)
        {
            var requiredKey = await serverSettings.GetSettingAsync(guildId, QuoteSettings.QuoteApiKey);
            if (requiredKey == null || requiredKey != key)
                return StatusCode(403);

            var discord = facade.Client;
            var guild = discord.GetGuild(guildId);
            if (guild == null)
                return StatusCode(404);

            var forumChannels = guild.ForumChannels;
            var result = new List<ForumChannel>(forumChannels.Count);


            foreach (var forumChannel in forumChannels)
            {
                var active = await forumChannel.GetActiveThreadsAsync();
                var archived = await forumChannel.GetPublicArchivedThreadsAsync();

                var threads = new List<ForumThread>(active.Count + archived.Count);
                foreach (var thread in active)
                    threads.Add(new ForumThread(thread.Id, thread.Name, thread.CreatedAt, thread.IsArchived));
                foreach (var thread in archived)
                    threads.Add(new ForumThread(thread.Id, thread.Name, thread.CreatedAt, thread.IsArchived));

                result.Add(new ForumChannel(forumChannel.Id, forumChannel.Name, forumChannel.CreatedAt, threads));
            }

            return new JsonResult(result);
        }
    }
}
