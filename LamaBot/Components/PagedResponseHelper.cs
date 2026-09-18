using Discord;
using Discord.WebSocket;
using LamaBot.Modules.Quotes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LamaBot.Components
{
    public abstract class PagedResponseHelper<TItem, TParams>
    {
        public record PageParams(ulong GuildId, int Page, TParams Params);

        protected readonly IInteractiveComponentService _componentService;

        protected PagedResponseHelper(IInteractiveComponentService componentService)
        {
            _componentService = componentService ?? throw new ArgumentNullException(nameof(componentService));
        }

        protected abstract Task<IReadOnlyList<TItem>> GetItemsAsync(ulong guildId, TParams options);
        protected abstract EmbedFieldBuilder GetField(TItem item);

        private async Task ShowPageAsync(SocketMessageComponent component, object? data)
        {
            var param = data as PageParams;
            if (param == null)
                return;

            await component.UpdateAsync(async (msg) =>
            {
                await ShowPageResultsAsync(msg, param);
            });
        }

        public async Task ShowFirstPage(MessageProperties message, ulong guildId, TParams options)
        {
            await ShowPageResultsAsync(message, new PageParams(guildId, 0, options));
        }

        private async Task ShowPageResultsAsync(MessageProperties message, PageParams searchParams)
        {
            // Get total amount of items
            var totalItems = await GetItemsAsync(searchParams.GuildId, searchParams.Params);

            // Split up into current page
            var pageCount = (int)Math.Ceiling((float)totalItems.Count / Constants.QuoteSearchPageSize);
            if (pageCount == 0)
                pageCount = 1;
            var page = Math.Clamp(searchParams.Page, 0, pageCount - 1);
            var pagedItems = totalItems.Skip(page * Constants.QuoteSearchPageSize).Take(Constants.QuoteSearchPageSize).ToList();

            // Create the search result embed
            var embed = new EmbedBuilder()
                    .WithTitle($"Found {totalItems.Count} results (page {page + 1}/{pageCount})");
            if (totalItems.Count > 0)
            {
                foreach (var item in pagedItems)
                    embed.AddField(GetField(item));
            }
            else
            {
                embed.WithDescription("No results found");
            }

            message.Embed = embed.Build();

            // Create page navigation buttons
            var component = new ComponentBuilder();
            var previousPageId = _componentService.Register(searchParams with { Page = page - 1 }, ShowPageAsync);
            component.WithButton(label: "Previous", customId: previousPageId, disabled: page == 0);
            var nextPageId = _componentService.Register(searchParams with { Page = page + 1 }, ShowPageAsync);
            component.WithButton(label: "Next", customId: nextPageId, disabled: page + 1 >= pageCount);

            message.Components = component.Build();
        }
    }
}
