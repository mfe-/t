using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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
            string command = String.Join(" ", args.Skip(1));
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

        private async Task ProcessEntertedCommand(string enteredCommand, string[] param)
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
                default:
                    ShowOptions();
                    break;
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
                //add sendung via helper
                return new string[0];
            }
            else
            {
                //add sendung via params
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
    }
}
