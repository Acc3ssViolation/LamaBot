using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using LamaBot.Components;
using LamaBot.Servers;
using System.Text.Json;

namespace LamaBot.Quotes
{
    [CommandContextType(InteractionContextType.Guild)]
    [Group("quote", "Once you put something on the internet it's there forever - Sun Zu")]
    public class QuoteInteractionModule : InteractionModuleBase
    {
        private readonly HttpClient _httpClient;
        private readonly IQuoteRepository _quoteRepository;
        private readonly IInteractiveComponentService _componentService;
        private readonly ILogger<QuoteInteractionModule> _logger;

        public QuoteInteractionModule(HttpClient httpClient, IQuoteRepository quoteRepository, IInteractiveComponentService componentService, ILogger<QuoteInteractionModule> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _quoteRepository = quoteRepository ?? throw new ArgumentNullException(nameof(quoteRepository));
            _componentService = componentService ?? throw new ArgumentNullException(nameof(componentService));
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

        [RequireUserPermission(GuildPermission.ManageMessages)]
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

        public record UberQuote(int Id, string Nick, ulong UserId, string Channel, ulong Server, string Text, ulong MessageId, ulong Time, DateTimeOffset DateTime);
        [RequireOwner]
        [SlashCommand("import", "Import quotes from that other bot")]
        public async Task ImportQuotesAsync(IAttachment attachment)
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
                var jsonString = await _httpClient.GetStringAsync(attachment.Url);
                var quotes = JsonSerializer.Deserialize<List<UberQuote>>(jsonString, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                if (quotes == null)
                    throw new ArgumentNullException(nameof(quotes));

                async Task ReportProgressAsync(string message)
                {
                    await ModifyOriginalResponseAsync((msg) =>
                    {
                        msg.Content = $"Processing quote list\n\n{message}";
                    });
                }

                for (var i = 0; i < quotes.Count; i++)
                {
                    var uberQuote = quotes[i];
                    var client = (DiscordSocketClient)Context.Client;
                    var guild = client.Guilds.FirstOrDefault(g => g.Id == uberQuote.Server);
                    if (guild == null)
                    {
                        _logger.LogDebug("Cannot find guild <{Id}>", uberQuote.Server);
                        continue;
                    }

                    var channel = guild.Channels.FirstOrDefault(c => c.Name == uberQuote.Channel);
                    var channelId = channel?.Id ?? 0;

                    var quote = new Quote(guild.Id, uberQuote.Id, uberQuote.UserId, uberQuote.Nick, channelId, uberQuote.Channel ?? "", uberQuote.Text, uberQuote.MessageId, uberQuote.DateTime.ToUniversalTime().DateTime);
                    await _quoteRepository.InsertQuoteAsync(quote);
                    if (i % 50 == 0)
                        await ReportProgressAsync($"Adding quotes {i * 100 / quotes.Count}% - '{quote.Content}'");
                }
                await ReportProgressAsync($"Adding quotes completed! Highest quote is #{quotes.Max(q => q.Id)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import quotes");
                await this.OnDeferredErrorAsync(ex).ConfigureAwait(false);
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [SlashCommand("export", "Export quotes to a file")]
        public async Task ExportQuotesAsync()
        {
            var guildId = Context.Interaction.GuildId;
            if (!guildId.HasValue)
            {
                await RespondAsync("This command only be run in a server");
                return;
            }

            await DeferAsync(ephemeral: true);

            var quotes = await _quoteRepository.GetQuotesAsync(guildId.Value);

            try
            {
                // The FileAttachment will handle the disposing
                var ms = new MemoryStream();

                // Loading them all into memory...
                await JsonSerializer.SerializeAsync(ms, quotes, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });

                await ModifyOriginalResponseAsync((msg) =>
                {
                    var attachment = new FileAttachment(ms, $"quotes-{guildId.Value}-{DateTime.UtcNow:s}.json");
                    msg.Attachments = new Optional<IEnumerable<FileAttachment>>([attachment]);
                    msg.Content = $"Exported {quotes.Count} quotes";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export quotes");
                await this.OnDeferredErrorAsync(ex).ConfigureAwait(false);
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Group("emoji", "Configure the emoji reaction quote thing")]
        public class QuoteEmojiInteractionModule : InteractionModuleBase
        {
            private readonly IServerSettings _serverSettings;

            public QuoteEmojiInteractionModule(IServerSettings serverSettings)
            {
                _serverSettings = serverSettings ?? throw new ArgumentNullException(nameof(serverSettings));
            }

            [SlashCommand("get", "Get the currently configured emoji")]
            public async Task GetEmojiAsync()
            {
                var emoji = await _serverSettings.GetSettingAsync(Context.Guild.Id, QuoteSettings.QuoteEmoji);
                if (emoji == null)
                {
                    await RespondAsync("Reaction based quoting is disabled on this server");
                    return;
                }

                await RespondAsync($"Reacting with {emoji} will trigger a quote");
            }

            [SlashCommand("set", "Set the emoji")]
            public async Task GetEmojiAsync(
                [Summary("emoji", "The emoji to trigger a quote with")] string emoji
                )
            {
                await DeferAsync();
                await _serverSettings.SetSettingAsync(Context.Guild.Id, QuoteSettings.QuoteEmoji, emoji);
                await ModifyOriginalResponseAsync(msg =>
                {
                    msg.Content = $"Reacting with {emoji} will trigger a quote";
                });
            }

            [SlashCommand("clear", "Disable quoting via emoji")]
            public async Task ClearEmojiAsync()
            {
                await DeferAsync();
                await _serverSettings.ClearAsync(Context.Guild.Id, QuoteSettings.QuoteEmoji);
                await ModifyOriginalResponseAsync(msg =>
                {
                    msg.Content = "Reaction based quoting is now disabled on this server";
                });
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Group("daily", "Configure the quote of the day")]
        public class QuoteOfTheDayInteractionModule : InteractionModuleBase
        {
            private readonly IServerSettings _serverSettings;

            public QuoteOfTheDayInteractionModule(IServerSettings serverSettings)
            {
                _serverSettings = serverSettings ?? throw new ArgumentNullException(nameof(serverSettings));
            }

            [SlashCommand("get", "Get the currently configured QOTD channel")]
            public async Task GetChannelAsync()
            {
                var channelId = await _serverSettings.GetSettingAsync(Context.Guild.Id, QuoteSettings.QuoteOfTheDayChannel);
                if (channelId == null)
                {
                    await RespondAsync("The QOTD is disabled in this server");
                    return;
                }

                await RespondAsync($"The QOTD is sent in <#{channelId}>");
            }

            [SlashCommand("set", "Set the QOTD channel")]
            public async Task SetChannelAsync(
                [Summary("channel", "The channel in which to send the QOTD")] SocketTextChannel channel
                )
            {
                await DeferAsync();
                await _serverSettings.SetSettingAsync(Context.Guild.Id, QuoteSettings.QuoteOfTheDayChannel, channel.Id.ToString());
                await ModifyOriginalResponseAsync(msg =>
                {
                    msg.Content = $"The QOTD is sent in <#{channel.Id}>";
                });
            }

            [SlashCommand("clear", "Disable the QOTD")]
            public async Task ClearChannelAsync()
            {
                await DeferAsync();
                await _serverSettings.ClearAsync(Context.Guild.Id, QuoteSettings.QuoteOfTheDayChannel);
                await ModifyOriginalResponseAsync(msg =>
                {
                    msg.Content = "QOTD is now disabled on this server";
                });
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
                var quote = new Quote(guildChannel.GuildId, 0, message.Author.Id, message.Author.Username, message.Channel.Id, message.Channel.Name, message.Content, message.Id, message.Timestamp.UtcDateTime);
                quote = await _quoteRepository.AddQuoteAsync(quote).ConfigureAwait(false);
                await ModifyOriginalResponseAsync((msg) =>
                {
                    msg.Content = $"<@{Context.User.Id}> added quote #{quote.Id} {quote.GetMessageLink()}";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add quote");
                await this.OnDeferredErrorAsync(ex).ConfigureAwait(false);
            }
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [MessageCommand("Update quote")]
        public async Task UpdateQuoteAsync(IMessage message)
        {
            if (message.Channel is not IGuildChannel guildChannel)
            {
                await RespondAsync("Quotes can only be added via servers");
                return;
            }

            await DeferAsync();

            try
            {
                var quote = await _quoteRepository.UpdateQuoteAsync(guildChannel.GuildId, message.Id, message.Content).ConfigureAwait(false);
                await ModifyOriginalResponseAsync((msg) =>
                {
                    if (quote != null)
                        msg.Content = $"<@{Context.User.Id}> update quote #{quote.Id} {quote.GetMessageLink()}";
                    else
                        msg.Content = $"<@{Context.User.Id}> tried to update a quote, but the message was never quoted!";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update quote");
                await this.OnDeferredErrorAsync(ex).ConfigureAwait(false);
            }
        }

        [SlashCommand("create", "Just make something up")]
        public async Task CreateQuoteAsync(
            [Summary("text", "What did they say?!")] string content,
            [Summary("author", "Who said that?")] string author
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
                var msg = await Context.Interaction.GetOriginalResponseAsync();
                var authorUser = (Context.Guild as SocketGuild)?.ResolveUser(author);
                var quote = new Quote(guildId.Value, 0, authorUser?.Id ?? 0, authorUser?.Username ?? author, Context.Channel.Id, Context.Channel.Name, content, msg.Id, DateTime.UtcNow);
                quote = await _quoteRepository.AddQuoteAsync(quote).ConfigureAwait(false);
                await ModifyOriginalResponseAsync((msg) =>
                {
                    msg.Content = $"<@{Context.User.Id}> added quote #{quote.Id} {quote.GetMessageLink()}";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add quote");
                await this.OnDeferredErrorAsync(ex).ConfigureAwait(false);
            }
        }

        private record QuoteSearchParams(ulong GuildId, SocketGuildUser? User, string? Content, int Page);

        private async Task SearchQuotesAsync(SocketMessageComponent component, object? data)
        {
            var param = data as QuoteSearchParams;
            if (param == null)
                return;
            
            await component.UpdateAsync(async (msg) =>
            {
                await ShowQuoteSearchResultsAsync(msg, param);
            });
        }

        [SlashCommand("search", "Hey Google!")]
        public async Task SearchQuotesAsync(
            [Summary("author", "Who said the thing?")] SocketGuildUser? user = null,
            [Summary("content", "What was it about?")] string? content = null)
        {
            var guildId = Context.Interaction.GuildId;
            if (!guildId.HasValue)
            {
                await RespondAsync("This command only be run in a server");
                return;
            }

            await DeferAsync(ephemeral: true);

            await ModifyOriginalResponseAsync(async (msg) =>
            {
                await ShowQuoteSearchResultsAsync(msg, new QuoteSearchParams(guildId.Value, user, content, 0));
            });
        }

        private async Task ShowQuoteSearchResultsAsync(MessageProperties message, QuoteSearchParams searchParams)
        {
            // Get quotes that match filter
            IEnumerable<Quote> quotes = await _quoteRepository.GetQuotesAsync(searchParams.GuildId);
            if (searchParams.User != null)
                quotes = quotes.Where(q => q.UserId == searchParams.User.Id || q.UserName == searchParams.User.Username);
            if (searchParams.Content != null)
                quotes = quotes.Where(q => q.Content.Contains(searchParams.Content, StringComparison.OrdinalIgnoreCase));
            var filteredQuotes = quotes.ToList();

            // Split up into current page
            var pageCount = (int)Math.Ceiling((float)filteredQuotes.Count / Constants.QuoteSearchPageSize);
            if (pageCount == 0)
                pageCount = 1;
            var page = Math.Clamp(searchParams.Page, 0, pageCount - 1);
            var pagedQuotes = filteredQuotes.Skip(page * Constants.QuoteSearchPageSize).Take(Constants.QuoteSearchPageSize).ToList();

            // Create the search result embed
            var embed = new EmbedBuilder()
                    .WithTitle($"Found {filteredQuotes.Count} quotes (page {page + 1}/{pageCount})");
            if (filteredQuotes.Count > 0)
            {
                foreach (var quote in pagedQuotes)
                {
                    embed.AddField($"#{quote.Id}", QuoteToField(quote));
                }
            }
            else
            {
                embed.WithDescription("No quotes found");
            }

            message.Embed = embed.Build();

            // Create page navigation buttons
            var component = new ComponentBuilder();
            var previousPageId = _componentService.Register(searchParams with { Page = page - 1 }, SearchQuotesAsync);
            component.WithButton(label: "Previous", customId: previousPageId, disabled: page == 0);
            var nextPageId = _componentService.Register(searchParams with { Page = page + 1 }, SearchQuotesAsync);
            component.WithButton(label: "Next", customId: nextPageId, disabled: page + 1 >= pageCount);

            message.Components = component.Build();
        }

        [SlashCommand("info", "Show info about a quote")]
        public async Task QuoteUsageAsync(
            [Summary("quoteId", "The ID of the quote to get info for")] int quoteId)
        {
            var guildId = Context.Interaction.GuildId;
            if (!guildId.HasValue)
            {
                await RespondAsync("This command only be run in a server");
                return;
            }

            await DeferAsync();

            var quote = await _quoteRepository.GetQuoteAsync(guildId.Value, quoteId);
            if (quote == null)
            {
                await ModifyOriginalResponseAsync((message) =>
                {
                    message.Content = "Quote not found? Perhaps the archives are incomplete.";
                });
                return;
            }

            var requestCount = await _quoteRepository.GetQuoteRequestCountAsync(guildId.Value, quoteId);
            var firstRequest = await _quoteRepository.GetFirstQuoteUseAsync(guildId.Value, quoteId);
            var lastRequest = await _quoteRepository.GetLastQuoteUseAsync(guildId.Value, quoteId);
            var biggestRequesterId = await _quoteRepository.GetMostCommonRequesterAsync(guildId.Value, quoteId);

            await ModifyOriginalResponseAsync((message) =>
            {
                var embed = new EmbedBuilder()
                    .WithTitle($"Info for quote #{quoteId}")
                    .AddField("Author", quote.GetQuoteAuthor(), inline: true)
                    .AddField("Message", quote.GetMessageLink(), inline: true)
                    .AddField("Use count", requestCount, inline: true)
                    .AddField("First use", firstRequest?.ToDiscordTimestamp() ?? "Never", inline: true)
                    .AddField("Last use", lastRequest?.ToDiscordTimestamp() ?? "Never", inline: true)
                    .AddField("Biggest fan", biggestRequesterId.HasValue ? $"<@{biggestRequesterId}>" : "Nobody :(", inline: true);

                message.Embed = embed.Build();
            });
        }

        [SlashCommand("top", "See who is truly popular")]
        public async Task TopQuotesAsync(
            [Summary("user", "This user's most used quotes")] SocketGuildUser? user = null,
            [Summary("max", "Amount of quotes to get, defaults to 10")] int max = 10)
        {
            var guildId = Context.Interaction.GuildId;
            if (!guildId.HasValue)
            {
                await RespondAsync("This command only be run in a server");
                return;
            }

            if (max > 25)
            {
                await RespondAsync("Top 25 is the best we can do, sorry");
                return;
            }

            if (user != null && user.Id != Context.Interaction.User.Id)
            {
                var info = await Context.Client.GetApplicationInfoAsync();
                if (Context.Interaction.User.Id != info.Owner.Id)
                {
                    await RespondAsync("Privacy law says you can't see this");
                    return;
                }
            }

            await DeferAsync();

            var quotes = await _quoteRepository.GetQuotesByRequestCountAsync(guildId.Value, max, user?.Id);

            await ModifyOriginalResponseAsync((msg) =>
            {
                var embed = new EmbedBuilder()
                    .WithTitle(user != null ? $"Top {quotes.Count} quotes used by {user.DisplayName}" : $"Top {quotes.Count} quotes");
                if (quotes.Count == 0)
                    embed.WithDescription("No quotes found");
                else
                    for (var i = 0; i < quotes.Count; i++)
                    {
                        var nr = i + 1;
                        var q = quotes[i];
                        embed.AddField($"#{nr}  -  Quote {q.Quote.Id}  -  {q.Count}x", QuoteToField(q.Quote));
                    }

                msg.Embed = embed.Build();
            });
        }

        private static string QuoteToField(Quote quote)
        {
            const string Elipses = "...";

            var content = quote.Content;
            var field = $"{content}\n- {quote.GetQuoteAuthor()} {quote.GetMessageLink()}";
            var overshoot = field.Length - 1024;
            if (overshoot > 0)
                field = $"{content[..^(overshoot + Elipses.Length)]}{Elipses}\n- {quote.GetQuoteAuthor()} {quote.GetMessageLink()}";
            return field;
        }
    }
}
