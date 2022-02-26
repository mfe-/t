namespace t.App.Service
{
    internal static class Mapper
    {
        public static Models.Player ToPlayer(lib.Game.Player player)
        {
            return new Models.Player(player.Name, player.PlayerId) { Points = player.Points };
        }
    }
}