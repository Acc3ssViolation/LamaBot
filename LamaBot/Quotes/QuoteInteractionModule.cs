using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;

namespace LamaBot.Quotes
{
    [CommandContextType(InteractionContextType.Guild)]
    [Group("quote", "Once you put something on the internet it's there forever - Sun Zu")]
    internal class QuoteInteractionModule : InteractionModuleBase
    {
        private readonly IQuoteRepository _quoteRepository;
        private readonly ILogger<QuoteInteractionModule> _logger;

        public QuoteInteractionModule(IQuoteRepository quoteRepository, ILogger<QuoteInteractionModule> logger)
        {
            _quoteRepository = quoteRepository ?? throw new ArgumentNullException(nameof(quoteRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [SlashCommand("get", "Get a random quote")]
        public async Task GetRandomQuoteAsync(
            [Summary("user", "Get a quote from a specific user")] IUser? user = null
            )
        {
            var guildId = Context.Interaction.GuildId;
            if (!guildId.HasValue)
            {
                await RespondAsync("This command only be run in a server");
                return;
            }

            await DeferAsync();

            try
            {
                var userName = user?.Username ?? null;
                var quote = await _quoteRepository.GetRandomQuoteAsync(guildId.Value, userName);

                await ModifyOriginalResponseAsync((msg) =>
                {
                    if (quote != null)
                    {
                        msg.Embed = quote.CreateEmbed();
                    }
                    else
                    {
                        msg.Content = "No quote found lol";
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get random quote");
                await this.OnDeferredErrorAsync(ex).ConfigureAwait(false);
            }
        }

        [SlashCommand("delete", "Delete a quote")]
        public async Task DeleteQuoteAsync(
            [Summary("id", "The id of the quote to delete")] int quoteId
        )
        {
            var guildId = Context.Interaction.GuildId;
            if (!guildId.HasValue)
            {
                await RespondAsync("This command only be run in a server");
                return;
            }

            await DeferAsync();

            try
            {
                var deleted = await _quoteRepository.DeleteQuoteAsync(guildId.Value, quoteId);

                await ModifyOriginalResponseAsync((msg) =>
                {
                    if (deleted)
                    {
                        msg.Content = $"Quote {quoteId} deleted";
                    }
                    else
                    {
                        msg.Content = $"Quote {quoteId} could not be found";
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete quote");
                await this.OnDeferredErrorAsync(ex).ConfigureAwait(false);
            }
        }

        [MessageCommand("Add quote")]
        public async Task QuoteMessageAsync(IMessage message)
        {
            if (message.Channel is not IGuildChannel guildChannel)
            {
                await RespondAsync("Quotes can only be added via servers");
                return;
            }

            await DeferAsync();

            try
            {
                var quote = new Quote(guildChannel.GuildId, 0, message.Author.Id, message.Author.Username, message.Channel.Name, message.Content, message.Id, message.Timestamp.UtcDateTime);
                quote = await _quoteRepository.AddQuoteAsync(quote).ConfigureAwait(false);
                await ModifyOriginalResponseAsync((msg) =>
                {
                    msg.Content = $"Added quote {quote.Id}";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add quote");
                await this.OnDeferredErrorAsync(ex).ConfigureAwait(false);
            }
        }
    }

}
