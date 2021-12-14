using System.Collections.Generic;

namespace t.lib.Game.EventArgs
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
