using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using t.lib.EventArgs;
using t.lib.Game;

namespace t.lib.Console
{
    public class GameClientConsole : GameClient
    {
        public GameClientConsole(ILogger logger, IConfiguration configuration, Func<Task<string>> onCommandFunc) : base(logger, configuration, onCommandFunc)
        {

        }
        /// <summary>
        /// Parse StartArguments if provided
        /// </summary>
        /// <param name="args">StartArguments - can be empty if none are provided</param>
        /// <returns>Task which indicates the process of the current execution of the method</returns>
        public override async Task ParseStartArgumentsAsync(string[] args)
        {
            string command = String.Join(" ", args);
            string enteredCommand = PrepareCommandInput(command).Replace("-", "");
            string[] param = ToParam(command, enteredCommand);
            await ProcessEntertedCommand(enteredCommand, param);
        }
        public override async Task OnShowMenueAsync()
        {
            ShowOptions();

            string command = string.Empty;
            do
            {
                try
                {
                    System.Console.WriteLine("Please enter your command:");
                    command = await onChoiceCommandFunc();
                    string enteredCommand = PrepareCommandInput(command);
                    string[] param = ToParam(command, enteredCommand);
                    await ProcessEntertedCommand(enteredCommand, param);
                }
                catch (Exception e)
                {
                    logger.LogError(e, nameof(OnShowMenueAsync));
                }
            } while (command != "exit");

        }
        public override Task ShowAvailableCardsAsync(IEnumerable<Card> availableCards)
        {
            foreach (var card in availableCards)
            {
                System.Console.WriteLine($"Card {card.Value}");
            }
            System.Console.WriteLine("Press the number of the card you want to play.");
            return Task.CompletedTask;
        }
        private async Task ProcessEntertedCommand(string enteredCommand, string[] param)
        {
            try
            {
                switch (enteredCommand)
                {
                    case "exit":
                        break;
                    case "version":
                        System.Console.WriteLine(Assembly.GetExecutingAssembly().FullName);
                        break;
                    case "join":
                        string ipadress = (param.FirstOrDefault(a => a.Contains("ip")) ?? "").Replace("-ip=", "");
                        int.TryParse((param.FirstOrDefault(a => a.Contains("port")) ?? "").Replace("-port=", ""), out int port);
                        string playername = (param.FirstOrDefault(a => a.Contains("name")) ?? "").Replace("-name=", "");
                        await OnJoinLanGameAsync(ipadress, port, playername);
                        break;
                    case "find":
                        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        var publicGames = await GameSocketClient.FindLanGamesAsync(15000, cancellationTokenSource.Token);
                        await OnFoundLanGames(publicGames);
                        break;
                    default:
                        //interactive
                        await OnShowMenueAsync();
                        break;
                }
            }
            catch (SocketException se) when (se.ErrorCode == 10060)
            {
                System.Console.WriteLine($"Could not reach Server. {se.Message}");
            }
            catch (GameActionProtocolException e)
            {
                System.Console.WriteLine(e);
                logger.LogError(e, nameof(OnJoinLanGameAsync));
            }
        }

        private static void ShowOptions()
        {
            System.Console.WriteLine("Welcome to t");
            System.Console.WriteLine("join -ip=127.0.0.1 -port=11000 -name=martin");
            System.Console.WriteLine("version shows the version of the app");
            System.Console.WriteLine("exit the app");
        }

        private string PrepareCommandInput(string command)
        {
            command = command.TrimStart();
            return command.Split(" ").FirstOrDefault() ?? String.Empty;
        }
        public static string[] ToParam(string completeCommand, string commandInital)
        {
            completeCommand = ReplaceFirst(completeCommand, commandInital, string.Empty);
            if (String.IsNullOrEmpty(completeCommand))
            {
                return new string[0];
            }
            else
            {
                return completeCommand.Split(' ').Where(a => !String.IsNullOrEmpty(a)).ToArray();
            }
        }
        public static string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search, 0, StringComparison.OrdinalIgnoreCase);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        public override Task OnNextRoundAsync(NextRoundEventArgs e)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"Round: {e.Round} Playing for card: {e.Card.Value}");
            System.Console.ResetColor();
            return Task.CompletedTask;
        }

        public override Task ShowPlayerWon(IEnumerable<Player> playerStats)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"Player: {playerStats.First().Name} won with: {playerStats.First().Points}");
            ShowPlayerStats(playerStats);
            System.Console.ResetColor();
            return Task.CompletedTask;
        }

        public override Task ShowPlayerStats(IEnumerable<Player> playerStats)
        {
            foreach (var p in playerStats)
            {
                System.Console.WriteLine($"{p.Name,10} {$"{p.Points,2}"}");
            }
            return Task.CompletedTask;
        }

        public override Task ShowPlayerOffered(Player player, int offered, int forCard)
        {
            System.Console.WriteLine($"{player.Name} offered {offered} for {forCard}");
            return Task.CompletedTask;
        }

        public override Task OnFoundLanGames(IEnumerable<PublicGame> publicGames)
        {
            System.Console.WriteLine($"Found the following games:");
            int i = 1;
            var enumerator = publicGames.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var publicGame = enumerator.Current;
                publicGame.GameId = i;

                System.Console.WriteLine($"[{publicGame.GameId}] {publicGame.GameName} Players [{publicGame.CurrentAmountOfPlayers}/{publicGame.RequiredAmountOfPlayers}] GameRounds: {publicGame.GameRounds} {publicGame.ServerIpAddress}:{publicGame.ServerPort}");
            }

            return Task.CompletedTask;
        }
    }
}
