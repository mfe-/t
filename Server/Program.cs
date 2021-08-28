// See https://aka.ms/new-console-template for more information
using Server;
using System;
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

            game.RegisterPlayer(player2);
            game.RegisterPlayer(player1);

            game.Start();

            game.PlayerReport(player1, 2);
            game.PlayerReport(player2, 3);

            game.NextRound();

            game.PlayerReport(player1, 1);
            game.PlayerReport(player2, 8);

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


