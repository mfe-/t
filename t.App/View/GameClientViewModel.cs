using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using t.App.Models;
using t.lib;
using t.lib.Game;
using t.lib.Game.EventArgs;
using t.lib.Network;

namespace t.App.View
{
    public class GameClientViewModel : GameClient
    {
        public GameClientViewModel(ILogger logger, AppConfig appConfig, Func<Task<string>> onChoiceCommandFunc)
            : base(logger, appConfig, onChoiceCommandFunc)
        {

        }

        public override Task OnFoundLanGames(IEnumerable<PublicGame> publicGames)
        {
            throw new NotImplementedException();
        }

        public override Task OnNextRoundAsync(NextRoundEventArgs e)
        {
            throw new NotImplementedException();
        }

        public override Task OnShowMenueAsync()
        {
            throw new NotImplementedException();
        }

        public override Task ParseStartArgumentsAsync(string[] args)
        {
            throw new NotImplementedException();
        }

        public override Task ShowAvailableCardsAsync(IEnumerable<Card> availableCards)
        {
            throw new NotImplementedException();
        }

        public override Task ShowPlayerOffered(Player player, int offered, int forCard)
        {
            throw new NotImplementedException();
        }

        public override Task ShowPlayerStats(IEnumerable<Player> playerStats)
        {
            throw new NotImplementedException();
        }

        public override Task ShowPlayerWon(IEnumerable<Player> playerStats)
        {
            throw new NotImplementedException();
        }
    }
}
