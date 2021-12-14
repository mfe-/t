namespace t.lib.Game.EventArgs
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
