using System.Net;

namespace t.lib.Network
{
    public class PublicGame
    {
        public PublicGame(IPAddress ServerIpAddress, int ServerPort, int RequiredAmountOfPlayers, int currentAmountOfPlayers, int GameRounds, string GameName)
        {
            this.ServerIpAddress = ServerIpAddress;
            this.ServerPort = ServerPort;
            this.RequiredAmountOfPlayers = RequiredAmountOfPlayers;
            this.GameRounds = GameRounds;
            this.GameName = GameName;
            this.CurrentAmountOfPlayers = currentAmountOfPlayers;
        }

        public IPAddress ServerIpAddress { get; }
        public int ServerPort { get; }
        public int RequiredAmountOfPlayers { get; }
        public int GameRounds { get; }
        public string GameName { get; }
        public int CurrentAmountOfPlayers { get; internal set; }
        public int? GameId { get; set; }
    }
}