using System.Net;

namespace t.lib
{
    public record PublicGame(IPAddress ServerIpAddress, int ServerPort, string GameName);
}