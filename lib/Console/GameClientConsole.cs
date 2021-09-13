using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.lib.Console
{
    public class GameClientConsole : GameClient
    {
        public GameClientConsole(ILogger logger, string[] args, Func<Task> onCommandFunc) : base(logger, args, onCommandFunc)
        {
        }

        public override void OnShowMenue()
        {
            System.Console.WriteLine("Please enter your command:");
            System.Console.WriteLine("--join ");

        }
    }
}
