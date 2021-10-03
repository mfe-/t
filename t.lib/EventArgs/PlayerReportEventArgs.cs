using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t.lib.Game;

namespace t.lib.EventArgs
{
    public class PlayerReportEventArgs : System.EventArgs
    {
        public PlayerReportEventArgs(Player player, Card card)
        {
            Player = player;
            Card = card;
        }

        public Player Player { get; }
        public Card Card { get; }
    }
}
