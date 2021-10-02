namespace t.lib.Game
{
    public class GameAction
    {
        public GameAction(int round, Player player, int offered, Card currentCard, bool roundfinished)
        {
            Round = round;
            Player = player;
            Offered = offered;
            ForCard = currentCard;
            RoundFinished = roundfinished;
        }
        public int Round { get; private set; }
        public Player Player { get; private set; }
        public int Offered { get; private set; }
        public Card ForCard { get; private set; }
        public bool RoundFinished { get; set; }
    }
}
