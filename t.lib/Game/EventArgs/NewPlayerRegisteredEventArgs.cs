namespace t.lib.Game.EventArgs
{
    public class NewPlayerRegisteredEventArgs
    {
        public NewPlayerRegisteredEventArgs(Player player)
        {
            Player = player;
        }

        public Player Player { get; }
    }
}
