using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t.lib.Game;

namespace t.lib.EventArgs
{
    public class PlayerJoinedEventArgs
    {
        public PlayerJoinedEventArgs(Player joinedPlayer,int requiredAmountOfPlayers)
        {
            JoinedPlayer = joinedPlayer;
            RequiredAmountOfPlayers = requiredAmountOfPlayers;
        }

        public Player JoinedPlayer { get; }
        public int RequiredAmountOfPlayers { get; }
    }
}
