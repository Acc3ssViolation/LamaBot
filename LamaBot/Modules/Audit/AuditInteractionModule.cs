using Discord;
using Discord.Interactions;
using LamaBot.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LamaBot.Modules.Audit
{
    public class AuditInteractionModule : InteractionModuleBase
    {
        private readonly IInteractiveComponentService _componentService;
        private readonly IAuditRepository _repository;
        private readonly ILogger<AuditInteractionModule> _logger;

        public AuditInteractionModule(IInteractiveComponentService componentService, IAuditRepository repository, ILogger<AuditInteractionModule> logger)
        {
            _componentService = componentService ?? throw new ArgumentNullException(nameof(componentService));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private class AuditHelper : PagedResponseHelper<AuditEntry, object>
        {
            private readonly IAuditRepository _repository;

            public AuditHelper(IAuditRepository repository, IInteractiveComponentService componentService) : base(componentService)
            {
                _repository = repository;
            }

            protected override EmbedFieldBuilder GetField(AuditEntry item)
            {
                return new EmbedFieldBuilder()
                    .WithName($"{item.TimestampUtc:yyyy-MM-dd HH:mm:ss}")
                    .WithValue($"<@{item.UserId}> - `{item.Module}` \"{item.Message}\"");
            }

            protected override async Task<IReadOnlyList<AuditEntry>> GetItemsAsync(ulong guildId, object options)
            {
                return await _repository.GetAuditEntriesAsync(guildId != 0 ? guildId : null, null, null);
            }
        }

        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [SlashCommand("audit", "Show audit logs for this server")]
        public async Task ShowAuditAsync()
        {
            var guildId = Context.Interaction.GuildId ?? 0;

            await DeferAsync(ephemeral: true);

            try
            {
                await ModifyOriginalResponseAsync(async (msg) =>
                {
                    var helper = new AuditHelper(_repository, _componentService);
                    await helper.ShowFirstPage(msg, guildId, this);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get audit log");
                await this.OnDeferredErrorAsync(ex).ConfigureAwait(false);
            }
        }
    }
}
