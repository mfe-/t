using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.lib.EventArgs
{
    public class GameStartedEventArgs
    {
        public GameStartedEventArgs(int totalPoints)
        {
            TotalPoints = totalPoints;
        }
        public int TotalPoints { get; }
    }
}
