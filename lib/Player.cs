using System.Diagnostics;

namespace t.lib
{
    [DebuggerDisplay("Name={Name}, Points={Points}")]
    public class Player
    {
        public Player(string name)
        {
            Name = name;
        }
        public string Name { get; set; }
        public int Points { get; set; }
    }
}