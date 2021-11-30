using System.Diagnostics;

namespace t.lib.Game
{
    [DebuggerDisplay("Round={Round},{Player}={Player.Name},Offered={Offered},ForCard={ForCard}")]
    public class GameAction
    {
        public GameAction(int round,int gameRound, Player player, int offered, Card currentCard, bool roundfinished)
        {
            Round = round;
            Player = player;
            Offered = offered;
            ForCard = currentCard;
            RoundFinished = roundfinished;
            GameRound = gameRound;
        }
        public int GameRound { get; set; }
        public int Round { get; private set; }
        public Player Player { get; private set; }
        public int Offered { get; private set; }
        public Card ForCard { get; private set; }
        public bool RoundFinished { get; set; }
    }
}
