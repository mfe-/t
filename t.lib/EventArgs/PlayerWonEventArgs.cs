using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t.lib.Game;

namespace t.lib.EventArgs
{
    public class PlayerWonEventArgs
    {
        public PlayerWonEventArgs(IEnumerable<Player> players)
        {
            Players = players;
        }

        public IEnumerable<Player> Players { get; }
    }
}
