// See https://aka.ms/new-console-template for more information
using Server;
using t.lib;

namespace t.Server
{
    public class Program
    {
        static SocketListener socketListener;
        static Thread thread;
        public static int Main(String[] args)
        {
            Game game = new Game();
            game.NewGame();

            Player player1 = new Player("martin");
            Player player2 = new Player("simon");
            Player player3 = new Player("katharina");

            game.RegisterPlayer(player2);
            game.RegisterPlayer(player1);

            game.Start(10);

            game.PlayerReport(player1, new Card(1));
            game.PlayerReport(player2, new Card(2));
            game.PlayerReport(player3, new Card(2));

            game.NextRound();

            game.PlayerReport(player1, new Card(2));
            game.PlayerReport(player2, new Card(1));

            game.NextRound();

            game.PlayerReport(player1, new Card(3));
            game.PlayerReport(player2, new Card(3));

            game.NextRound();
            game.PlayerReport(player1, new Card(4));
            game.PlayerReport(player2, new Card(5));

            game.NextRound();
            game.PlayerReport(player1, new Card(5));
            game.PlayerReport(player2, new Card(6));

            game.NextRound();
            game.PlayerReport(player1, new Card(6));
            game.PlayerReport(player2, new Card(5));

            game.NextRound();

            game.PlayerReport(player1, new Card(7));
            game.PlayerReport(player2, new Card(8));

            game.NextRound();
            game.PlayerReport(player1, new Card(8));
            game.PlayerReport(player2, new Card(7));

            game.NextRound();
            game.PlayerReport(player1, new Card(9));
            game.PlayerReport(player2, new Card(10));

            game.NextRound();
            game.PlayerReport(player1, new Card(10));
            game.PlayerReport(player2, new Card(9));

            game.NextRound();

            //socketListener = new SocketListener();
            //thread = new Thread((obj) =>
            //{
            //    socketListener.StartListening();
            //});
            //thread.Start();
            //while (true)
            //{
            //    string? enter = Console.ReadLine();
            //    if (enter == "exit") break;
            //}
            return 0;
        }
    }
}


