using Discord;
using Discord.Net;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamaBot
{
    internal class CommandRegistry
    {
        private readonly DiscordSocketClient _client;
        private readonly SocketGuild? _guild;

        public CommandRegistry(DiscordSocketClient client, SocketGuild? guild)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _guild = guild;

            _client.SlashCommandExecuted += OnSlashCommandExecuted;
        }

        private async Task OnSlashCommandExecuted(SocketSlashCommand arg)
        {
            await arg.RespondAsync("Hello there!").ConfigureAwait(false);
        }

        public async Task RegisterCommandsAsync()
        {
            var command = new SlashCommandBuilder()
                .WithName("llama")
                .WithDescription("Not an alpaca, but it will do")
                .WithDefaultPermission(true)
                .Build();

            await RegisterGuildCommandAsync(command);
        }

        private async Task<bool> RegisterGlobalCommandAsync(SlashCommandProperties command)
        {
            try
            {
                await _client.CreateGlobalApplicationCommandAsync(command).ConfigureAwait(false);
                return true;
            }
            catch (HttpException exc)
            {
                Console.WriteLine(exc.Message);
                return false;
            }
        }

        private async Task<bool> RegisterGuildCommandAsync(SlashCommandProperties command)
        {
            if (_guild == null)
                throw new InvalidOperationException("Guild is not set");

            try
            {
                await _guild.CreateApplicationCommandAsync(command).ConfigureAwait(false);
                return true;
            }
            catch (HttpException exc)
            {
                Console.WriteLine(exc.Message);
                return false;
            }
        }
    }
}
