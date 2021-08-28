using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.lib
{
    public record GameAction(int Round, Player Player, int Offered, Card ForCard);
}
