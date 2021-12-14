using System.Collections.Generic;

namespace t.lib.Network
{
    /// <summary>
    /// Contains constant values for <see cref="GameActionProtocol.Phase"/>
    /// </summary>
    public static class PhaseConstants
    {
        /// <summary>
        /// Can be used to responde to a message (like the message was processed).  
        /// </summary>
        /// <remarks>Can be used by a client and a server</remarks>
        public static readonly byte Ok =             0b000001;
        /// <summary>
        /// To exchange the used version
        /// </summary>
        /// <remarks>Can be used by client and server</remarks>
        public static readonly byte Version =        0b000010;
        /// <summary>
        /// To broadcast that a player was kicked from the game
        /// </summary>
        /// <remarks>used by the server</remarks>
        public static readonly byte KickedPlayer =   0b000011;
        /// <summary>
        /// to tell the server that we would like to register as player
        /// </summary>
        /// <remarks>used by the client</remarks>
        public static readonly byte RegisterPlayer = 0b000100;
        /// <summary>
        /// Is used by the server to broadcast that a specific player joined the game
        /// </summary>
        /// <remarks>used by the server</remarks>
        public static readonly byte NewPlayer =      0b000101;
        /// <summary>
        /// tell the client that the game started
        /// </summary>
        /// <remarks>used by the server></remarksused>
        public static readonly byte StartGame =      0b000110;
        /// <summary>
        /// tell the client that a player reported the following game move
        /// </summary>
        /// <remarks>used by client</remarks>
        public static readonly byte PlayerReported = 0b000111;
        /// <summary>
        /// all player reported their moves. Server announces in responde the next round
        /// </summary>
        /// <remarks>used by server</remarks>
        public static readonly byte NextRound =      0b001000;
        /// <summary>
        /// player scored (can occour multiple times for each player)
        /// </summary>
        /// <remarks>used by server</remarks>
        public static readonly byte PlayerScored =   0b001001;
        /// <summary>
        /// report that the following players won
        /// </summary>
        /// <remarks>used by server</remarks>
        public static readonly byte PlayerWon =      0b001010;
        /// <summary>
        /// an error occoured. contains the error message
        /// </summary>
        /// <remarks>can be used by server and client</remarks>
        public static readonly byte ErrorOccoured =  0b111111;
        /// <summary>
        /// Is used by the server if we are waiting for players
        /// </summary>
        /// <remarks>used by server</remarks>
        public static readonly byte WaitingPlayers = 0b001011;
        /// <summary>
        /// Converts a <see cref="GameActionProtocol.Phase" /> to a user friendly string
        /// </summary>
        /// <param name="phase">The phase to convert</param>
        /// <returns>a user friendly string</returns>
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
