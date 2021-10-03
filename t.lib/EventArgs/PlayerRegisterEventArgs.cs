using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t.lib.Game;

namespace t.lib.EventArgs
{
    public class PlayerRegisterEventArgs : System.EventArgs
    {
        public PlayerRegisterEventArgs(Player player)
        {
            Player = player;
        }
        public Player Player { get; }
    }
}
