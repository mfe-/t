using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.lib
{
    public record NextRoundEventArgs
    {
        public NextRoundEventArgs(int round, Card card)
        {
            Round = round;
            Card = card;
        }
        public int Round { get; set; }
        public Card Card { get; set; }
    }
}
