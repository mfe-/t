using System.Collections.Generic;

namespace t.lib.Network
{
    public static class PhaseConstants
    {
        public static readonly byte Ok =             0b000001;
        public static readonly byte Version =        0b000010;
        public static readonly byte KickedPlayer =   0b000011;
        public static readonly byte RegisterPlayer = 0b000100;
        public static readonly byte NewPlayer =      0b000101;
        public static readonly byte StartGame =      0b000110;
        public static readonly byte PlayerReported = 0b000111;
        /// <summary>
        /// player scored (can occour multiple times for each player)
        /// </summary>
        public static readonly byte NextRound =      0b001000;
        public static readonly byte PlayerScored =   0b001001;
        public static readonly byte PlayerWon =      0b001010;
        public static readonly byte ErrorOccoured =  0b111111;
        public static readonly byte WaitingPlayers = 0b001011;

        public static string ToString(byte phase)
        {
            var keyValuePairs = new Dictionary<byte, string>()
            {
                 { Ok,nameof(Ok) }
                ,{ Version,nameof(Version) }
                ,{ KickedPlayer,nameof(KickedPlayer) }
                ,{ RegisterPlayer,nameof(RegisterPlayer) }
                ,{ NewPlayer,nameof(NewPlayer) }
                ,{ StartGame,nameof(StartGame) }
                ,{ PlayerReported,nameof(PlayerReported) }
                ,{ PlayerScored,nameof(PlayerScored) }
                ,{ NextRound,nameof(NextRound) }
                ,{ PlayerWon,nameof(PlayerWon) }
                ,{ ErrorOccoured,nameof(ErrorOccoured) }
                ,{ WaitingPlayers,nameof(WaitingPlayers) }

            };
            if (keyValuePairs.ContainsKey(phase)) return keyValuePairs[phase];
            return phase.ToString();
        }
    }
}
