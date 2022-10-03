using System;
using System.Diagnostics;

namespace t.lib.Game
{
    [DebuggerDisplay("Name={Name}, Points={Points}")]
    public class Player
    {
        public Player(string name, Guid playerid)
        {
            Name = name;
            PlayerId = playerid;
        }
        public virtual Guid PlayerId { get; }
        public virtual string Name { get; set; }
        public virtual int Points { get; set; }
    }
}