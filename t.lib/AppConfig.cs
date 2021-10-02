using System;

namespace t.lib
{
    public class AppConfig
    {
        public AppConfig()
        {
            //init some default values
            TotalPoints = 10;
            RequiredAmountOfPlayers = 10;
        }

        public int ServerPort { get; set; }
        public string? ServerIpAdress { get; set; }
        public Guid Identifier { get; set; }
        public int RequiredAmountOfPlayers { get; set; }
        public int TotalPoints { get; set; }
    }
}
