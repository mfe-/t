namespace t.lib.Game.EventArgs;

public class PlayerLeftEventArgs
{
    public PlayerLeftEventArgs(Player player)
    {
        Player = player;
    }

    public Player Player { get; }
}
