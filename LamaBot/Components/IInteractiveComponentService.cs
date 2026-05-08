using Discord.WebSocket;
using System.Threading.Tasks;

namespace LamaBot.Components
{
    public delegate Task ButtonInteractionCallback(SocketMessageComponent component, object? data);

    public interface IInteractiveComponentService
    {
        string Register<T>(T? data, ButtonInteractionCallback callback);
    }
}
