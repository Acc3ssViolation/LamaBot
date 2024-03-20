using Discord.Interactions;

namespace LamaBot
{
    internal static class InteractionModuleExtensions
    {
        public static async Task OnErrorAsync(this InteractionModuleBase module, Exception? exc = null, string? message = null)
        {
            var text = message ?? "Oopsie woopsie, something went wrong";
            if (exc != null)
                text += $"\n{exc.Message}";
            await module.Context.Interaction.RespondAsync(text);
        }

        public static async Task OnDeferredErrorAsync(this InteractionModuleBase module, Exception? exc = null, string? message = null)
        {
            await module.Context.Interaction.ModifyOriginalResponseAsync((msg) =>
            {
                var text = message ?? "Oopsie woopsie, something went wrong";
                if (exc != null)
                    text += $"\n{exc.Message}";
                msg.Content = text ;
            });
        }

    }
}
