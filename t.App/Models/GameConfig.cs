namespace t.App.Models;

public class GameConfig
{
    public GameConfig(string gamename,string playerName, int gameRounds,
        int requiredAmountOfPlayers, string serverIpAdress, int serverPort, int broadcastPort, 
        CancellationTokenSource cancellationTokenServer)
    {
        Gamename = gamename;
        PlayerName = playerName;
        GameRounds = gameRounds;
        RequiredAmountOfPlayers = requiredAmountOfPlayers;
        ServerIpAdress = serverIpAdress;
        ServerPort = serverPort;
        BroadcastPort = broadcastPort;
        CancellationTokenServer = cancellationTokenServer;
    }
    public string Gamename { get; set; }
    public string PlayerName { get; }
    public int GameRounds { get; set; }
    public int RequiredAmountOfPlayers { get; set; }

    public string ServerIpAdress { get; set; }

    public int ServerPort { get; set; }
    public int BroadcastPort { get; set; }
    public CancellationTokenSource CancellationTokenServer { get; }
    public Task? JoinGameTask { get; set; }
}

