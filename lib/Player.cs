using System;
using System.Diagnostics;

namespace t.lib
{
    [DebuggerDisplay("Name={Name}, Points={Points}")]
    public class Player
    {
        public Player(string name, Guid playerid)
        {
            Name = name;
            PlayerId = playerid;
        }
        public Guid PlayerId { get; set; }
        public string Name { get; set; }
        public int Points { get; set; }
    }
}