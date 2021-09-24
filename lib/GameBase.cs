using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

[assembly: InternalsVisibleTo("t.TestProject1")]
namespace t.lib
{
    public abstract class GameBase
    {
        protected readonly GameLogic _game;
        protected readonly ILogger _logger;
        protected Guid _guid;
        public GameBase(ILogger logger)
        {
            _game = new GameLogic();
            _logger = logger;
            ActionDictionary.Add(Constants.Ok, OnOk);
            ActionDictionary.Add(Constants.ErrorOccoured, OnProtocolError);
            ActionDictionary.Add(Constants.RegisterPlayer, OnPlayerRegister);
            ActionDictionary.Add(Constants.StartGame, OnStart);
        }
        protected virtual void OnPlayerRegister(GameActionProtocol gameActionProtocol)
        {
            if (gameActionProtocol.Phase != Constants.RegisterPlayer) throw new InvalidOperationException($"Expecting {nameof(gameActionProtocol)} to be in the phase {nameof(Constants.RegisterPlayer)}");

            Player player = GetPlayer(gameActionProtocol);
            if (!_game.Players.Any(a => a.PlayerId == player.PlayerId))
            {
                _game.RegisterPlayer(player);
            }
        }
        protected virtual void OnStart(GameActionProtocol gameActionProtocol)
        {
            int totalPoints = GetTotalPoints(gameActionProtocol);
            _game.Start(totalPoints);
        }
        protected virtual void OnOk(GameActionProtocol gameActionProtocol)
        {

        }
        protected Dictionary<byte, Action<GameActionProtocol>> ActionDictionary = new();

        protected virtual void OnMessageReceive(GameActionProtocol gameActionProtocol)
        {
            _logger.LogInformation("OnMessageReceive from {player} with Phase {Phase}", gameActionProtocol.PlayerId, gameActionProtocol.Phase);
            using (_logger.BeginScope(new Dictionary<string, object> {
                { nameof(gameActionProtocol.Phase), gameActionProtocol.Phase }
               ,{ nameof(gameActionProtocol.PlayerId), gameActionProtocol.PlayerId}
            }))
            {
                if (ActionDictionary.ContainsKey(gameActionProtocol.Phase))
                {
                    ActionDictionary[gameActionProtocol.Phase].Invoke(gameActionProtocol);
                }
                else
                {
                    ActionDictionary[Constants.ErrorOccoured].Invoke(gameActionProtocol);
                }
            }
        }
        protected virtual void OnProtocolError(GameActionProtocol gameActionProtocol)
        {

        }
        protected abstract void BroadcastMessage(GameActionProtocol gameActionProtocol);

        internal virtual GameActionProtocol GameActionProtocolFactory(byte phase, Player? player = null, string? message = null, int? number = null)
        {
            GameActionProtocol gameActionProtocol = new GameActionProtocol();
            gameActionProtocol.Version = Constants.Version;
            gameActionProtocol.PlayerId = _guid;
            gameActionProtocol.Phase = phase;
            if (gameActionProtocol.Phase == Constants.NewPlayer)
            {
                if (player == null) throw new ArgumentNullException(nameof(player), $"{nameof(Constants.NewPlayer)} requires argument {nameof(player)}");
                if (number == null) throw new ArgumentNullException(nameof(number), $"The parameter {nameof(number)} is required as it is used as {nameof(GameLogic.RequiredAmountOfPlayers)}");
                //encode playerid and playername into payload
                var playerid = player.PlayerId.ToByteArray();
                if (!player.Name.Contains(Environment.NewLine))
                {
                    player.Name = $"{player.Name}{Environment.NewLine}";
                }
                var playername = Encoding.ASCII.GetBytes(player.Name);
                var nbytes = BitConverter.GetBytes(number.Value);
                gameActionProtocol.PayloadSize = (byte)nbytes.Length;
                byte[] paypload = new byte[playerid.Length + playername.Length + nbytes.Length];
                int i = 0;
                for (; i < playerid.Length; i++)
                {
                    paypload[i] = playerid[i];
                }
                int a = 0;
                for (; a < playername.Length; i++, a++)
                {
                    paypload[i] = playername[a];
                }
                for (int b = 0; b < nbytes.Length; i++, b++)
                {
                    paypload[i] = nbytes[b];
                }
                gameActionProtocol.PayloadSize = (byte)paypload.Length;
                gameActionProtocol.Payload = paypload;
            }
            else if (gameActionProtocol.Phase == Constants.ErrorOccoured)
            {
                if (!String.IsNullOrEmpty(message))
                {
                    var paypload = Encoding.ASCII.GetBytes(message);
                    gameActionProtocol.PayloadSize = (byte)paypload.Length;
                    gameActionProtocol.Payload = paypload;
                }
            }
            else if (gameActionProtocol.Phase == Constants.RegisterPlayer)
            {
                if (player == null) throw new ArgumentNullException(nameof(player), $"{nameof(Constants.NewPlayer)} requires argument {nameof(player)}");
                gameActionProtocol.Payload = Encoding.ASCII.GetBytes($"{player.Name}{Environment.NewLine}");
            }
            else if (gameActionProtocol.Phase == Constants.StartGame)
            {
                if (number == null) throw new ArgumentNullException(nameof(number), $"{nameof(Constants.StartGame)} requires argument {nameof(number)}");
                var nbytes = BitConverter.GetBytes(number.Value);
                gameActionProtocol.PayloadSize = (byte)nbytes.Length;
                gameActionProtocol.Payload = nbytes;
            }
            else if (gameActionProtocol.Phase == Constants.Ok)
            {
                gameActionProtocol.Payload = new byte[0];
                gameActionProtocol.PayloadSize = (byte)gameActionProtocol.Payload.Length;
            }
            return gameActionProtocol;
        }
        internal int GetTotalPoints(GameActionProtocol gameActionProtocol)
        {
            if (gameActionProtocol.Phase != Constants.StartGame) throw new ArgumentException($"{nameof(Constants.StartGame)} required for argument {nameof(gameActionProtocol.Phase)}");
            int a = BitConverter.ToInt32(gameActionProtocol.Payload.AsSpan().Slice(0, gameActionProtocol.PayloadSize));
            return a;
        }
        internal int GetNumber(GameActionProtocol gameActionProtocol)
        {
            if (gameActionProtocol.Phase == Constants.NewPlayer)
            {
                var span = gameActionProtocol.Payload.AsSpan();

                int playerNameLength = GetPlayerNameLength(gameActionProtocol.PayloadSize, span);
                //slice from guid+playername position to the end of byte array
                var numberBytes = span.Slice(Marshal.SizeOf(typeof(Guid)) + playerNameLength, gameActionProtocol.PayloadSize - (Marshal.SizeOf(typeof(Guid)) + playerNameLength));
                return BitConverter.ToInt32(numberBytes.ToArray());
            }
            throw new NotImplementedException($"Not implemented {gameActionProtocol.Phase}");
        }
        public virtual Player GetPlayer(GameActionProtocol gameActionProtocol)
        {
            if (gameActionProtocol.Phase == Constants.NewPlayer) return GetNewPlayer(ref gameActionProtocol);
            if (gameActionProtocol.Phase == Constants.RegisterPlayer) return GetRegisterePlayer(ref gameActionProtocol);
            throw new NotImplementedException($"Not implemented {gameActionProtocol.Phase}");
        }

        /// <summary>
        /// Gets the <see cref="Player"/> if <see cref="GameActionProtocol.Phase"/> is <see cref="Constants.NewPlayer"/>
        /// </summary>
        /// <param name="gameActionProtocol"></param>
        /// <returns></returns>
        protected virtual Player GetNewPlayer(ref GameActionProtocol gameActionProtocol)
        {
            if (gameActionProtocol.Phase != Constants.NewPlayer) throw new ArgumentException($"{nameof(Constants.NewPlayer)} required for argument {nameof(gameActionProtocol.Phase)}");
            var span = gameActionProtocol.Payload.AsSpan();

            Guid playerId = new Guid(span.Slice(0, Marshal.SizeOf<Guid>()));

            int playerNameLength = GetPlayerNameLength(gameActionProtocol.PayloadSize, span);
            var playerNameBytes = span.Slice(Marshal.SizeOf(typeof(Guid)), playerNameLength);
            var playername = Encoding.ASCII.GetString(playerNameBytes);

            return new Player(playername, playerId);
        }

        private static int GetPlayerNameLength(int payloadSize, Span<byte> span)
        {
            //first part of payload is Guid - followed by name and number which is seperated by NewLine
            var playerNameAndNumberBytes = span.Slice(Marshal.SizeOf(typeof(Guid)), payloadSize - Marshal.SizeOf(typeof(Guid)));
            //look up Enviroment.NewLine
            Span<byte> bufferNewLine = stackalloc byte[2];
            int playerNameLength = 0;
            int newLineIterationHit = 0;
            do
            {
                if (playerNameAndNumberBytes[playerNameLength] == 13)
                {
                    bufferNewLine[0] = playerNameAndNumberBytes[playerNameLength];
                    newLineIterationHit = playerNameLength + 1;
                }
                else if (newLineIterationHit == playerNameLength && playerNameAndNumberBytes[playerNameLength] == 10)
                {
                    bufferNewLine[1] = playerNameAndNumberBytes[playerNameLength];
                }
                else
                {
                    //reset if new line not found
                    bufferNewLine[0] = 0;
                    newLineIterationHit = 0;
                }
                playerNameLength++;
            } while (playerNameLength < playerNameAndNumberBytes.Length && !IsNewLine(bufferNewLine));
            return playerNameLength;
        }

        private static bool IsNewLine(Span<byte> bufferNewLine)
        {
            //Encoding.UTF8.GetBytes(System.Environment.NewLine)
            return (bufferNewLine[0] == 13 && bufferNewLine[1] == 10);
        }

        /// <summary>
        /// Gets the <see cref="Player"/> if <see cref="GameActionProtocol.Phase"/> is <see cref="Constants.RegisterPlayer"/>
        /// </summary>
        /// <param name="gameActionProtocol"></param>
        /// <returns></returns>
        protected Player GetRegisterePlayer(ref GameActionProtocol gameActionProtocol)
        {
            if (gameActionProtocol.Phase != Constants.RegisterPlayer) throw new ArgumentException($"{nameof(Constants.RegisterPlayer)} required for argument {nameof(gameActionProtocol.Phase)}");
            string playername = Encoding.ASCII.GetString(gameActionProtocol.Payload).Replace(Environment.NewLine, String.Empty);
            Player player = new Player(playername, gameActionProtocol.PlayerId);
            return player;
        }
    }
}
