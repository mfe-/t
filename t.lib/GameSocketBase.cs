using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using t.lib.EventArgs;
using t.lib.Game;

[assembly: InternalsVisibleTo("t.TestProject1")]
namespace t.lib
{
    public abstract class GameSocketBase
    {
        protected Dictionary<byte, Func<GameActionProtocol, object?, Task>> ActionDictionary = new();
        protected readonly GameLogic _game;
        protected readonly ILogger _logger;
        protected Guid _guid;
        protected GameSocketBase(ILogger logger)
        {
            _game = new GameLogic();
            _logger = logger;
            ActionDictionary.Add(Constants.Ok, OnOkAsync);
            ActionDictionary.Add(Constants.ErrorOccoured, OnProtocolErrorAsync);
            ActionDictionary.Add(Constants.RegisterPlayer, OnPlayerRegisterAsync);
        }
        protected virtual GameLogic Game => _game;
        protected virtual Task OnPlayerRegisterAsync(GameActionProtocol gameActionProtocol, object? obj)
        {
            if (gameActionProtocol.Phase != Constants.RegisterPlayer) throw new InvalidOperationException($"Expecting {nameof(gameActionProtocol)} to be in the phase {nameof(Constants.RegisterPlayer)}");
            lock (Game)
            {
                Player player = GetPlayer(gameActionProtocol);
                if (!Game.Players.Any(a => a.PlayerId == player.PlayerId))
                {
                    Game.RegisterPlayer(player);
                }
            }
            return Task.CompletedTask;
        }
        protected virtual Task OnOkAsync(GameActionProtocol gameActionProtocol, object? obj) => Task.CompletedTask;

        protected virtual async Task OnMessageReceiveAsync(GameActionProtocol gameActionProtocol, object? obj)
        {
            _logger.LogTrace("OnMessageReceive from {player} with Phase {Phase}", gameActionProtocol.PlayerId, Constants.ToString(gameActionProtocol.Phase));
            using (_logger.BeginScope(new Dictionary<string, object> {
                { nameof(gameActionProtocol.Phase), gameActionProtocol.Phase }
               ,{ nameof(gameActionProtocol.PlayerId), gameActionProtocol.PlayerId}
            }))
            {
                if (ActionDictionary.ContainsKey(gameActionProtocol.Phase))
                {
                    await ActionDictionary[gameActionProtocol.Phase].Invoke(gameActionProtocol, obj);
                }
                else
                {
                    await ActionDictionary[Constants.ErrorOccoured].Invoke(gameActionProtocol, obj);
                }
            }
        }
        protected virtual Task OnProtocolErrorAsync(GameActionProtocol gameActionProtocol, object? obj) => Task.CompletedTask;
        /// <summary>
        /// Broadcasts a <see cref="GameActionProtocol"/> to every recipient
        /// </summary>
        /// <param name="gameActionProtocol">The protocol which should be broadcastet</param>
        protected abstract Task BroadcastMessageAsync(GameActionProtocol gameActionProtocol, object? obj);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual GameActionProtocol GameActionProtocolFactory(byte phase, Player? player = null, string? message = null, int? number = null, int? number2 = null, NextRoundEventArgs? nextRoundEventArgs = null)
        {
            GameActionProtocol gameActionProtocol = new GameActionProtocol();
            gameActionProtocol.Version = Constants.Version;
            gameActionProtocol.PlayerId = _guid;
            gameActionProtocol.Phase = phase;
            if (gameActionProtocol.Phase == Constants.NewPlayer || gameActionProtocol.Phase == Constants.KickedPlayer)
            {
                gameActionProtocol = GameActionProtocolNewPlayer(player, number, ref gameActionProtocol);
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
                if (number2 == null) throw new ArgumentNullException(nameof(number2), $"{nameof(Constants.StartGame)} requires argument {nameof(number2)} for the amount of game rounds which should be played");
                var nbytes = BitConverter.GetBytes(number.Value);
                var n2bytes = BitConverter.GetBytes(number2.Value);
                gameActionProtocol.PayloadSize = (byte)(nbytes.Length + n2bytes.Length);

                Span<byte> numberbytes = stackalloc byte[gameActionProtocol.PayloadSize];
                nbytes.AsSpan().CopyTo(numberbytes);
                n2bytes.AsSpan().CopyTo(numberbytes[4..]);

                gameActionProtocol.Payload = numberbytes.ToArray();

            }
            else if (gameActionProtocol.Phase == Constants.Ok)
            {
                gameActionProtocol.Payload = new byte[0];
                gameActionProtocol.PayloadSize = (byte)gameActionProtocol.Payload.Length;
            }
            else if (gameActionProtocol.Phase == Constants.NextRound)
            {
                if (nextRoundEventArgs == null) throw new ArgumentNullException(nameof(nextRoundEventArgs), $"{nameof(Constants.NextRound)} requries argument {nameof(nextRoundEventArgs)}");
                var cardbytes = BitConverter.GetBytes(nextRoundEventArgs.Card.Value);
                var roundbytes = BitConverter.GetBytes(nextRoundEventArgs.Round);
                byte[] payLoad = new byte[8];
                cardbytes.CopyTo(payLoad, 0);
                roundbytes.CopyTo(payLoad, 4);
                gameActionProtocol.Payload = payLoad;
                gameActionProtocol.PayloadSize = (byte)gameActionProtocol.Payload.Length;
            }
            else if (gameActionProtocol.Phase == Constants.PlayerReported)
            {
                if (number == null) throw new ArgumentNullException(nameof(number), $"{nameof(Constants.PlayerReported)} requires argument {nameof(number)}");
                var nbytes = BitConverter.GetBytes(number.Value);
                gameActionProtocol.PayloadSize = (byte)nbytes.Length;
                gameActionProtocol.Payload = nbytes;
            }
            else if (gameActionProtocol.Phase == Constants.PlayerScored)
            {
                if (number == null) throw new ArgumentNullException(nameof(number), $"{nameof(Constants.PlayerScored)} requires argument {nameof(number)}");
                if (player == null) throw new ArgumentNullException(nameof(player), $"{nameof(Constants.PlayerScored)} requires argument {nameof(player)}");
                var playerid = player.PlayerId.ToByteArray();
                var nbytes = BitConverter.GetBytes(number.Value);
                var payload = new byte[nbytes.Length + playerid.Length];
                playerid.CopyTo(payload, 0);
                nbytes.CopyTo(payload, Marshal.SizeOf(typeof(Guid)));
                gameActionProtocol.Payload = payload;
                gameActionProtocol.PayloadSize = (byte)payload.Length;
                return gameActionProtocol;
            }
            else if (gameActionProtocol.Phase == Constants.PlayerWon)
            {
                if (player == null) throw new ArgumentNullException(nameof(player), $"{nameof(Constants.PlayerWon)} requires argument {nameof(player)}");
                var playerid = player.PlayerId.ToByteArray();
                gameActionProtocol.Payload = playerid;
                gameActionProtocol.PayloadSize = (byte)gameActionProtocol.Payload.Length;
            }
            else if (gameActionProtocol.Phase == Constants.WaitingPlayers)
            {
                gameActionProtocol.Payload = new byte[0];
            }
            return gameActionProtocol;
        }

        private static GameActionProtocol GameActionProtocolNewPlayer(Player? player, int? number, ref GameActionProtocol gameActionProtocol)
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
            return gameActionProtocol;
        }

        internal (int totalpoints,int totalgameRounds) GetGameStartValues(GameActionProtocol gameActionProtocol)
        {
            if (gameActionProtocol.Phase != Constants.StartGame) throw new ArgumentException($"{nameof(Constants.StartGame)} required for argument {nameof(gameActionProtocol.Phase)}");
            
            var p = BitConverter.ToInt32(gameActionProtocol.Payload.AsSpan().Slice(0, 4));
            var r = BitConverter.ToInt32(gameActionProtocol.Payload.AsSpan().Slice(4));
            return (p,r);
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
            if (gameActionProtocol.Phase == Constants.StartGame || gameActionProtocol.Phase == Constants.PlayerReported)
            {
                var span = gameActionProtocol.Payload.AsSpan();
                return BitConverter.ToInt32(span);
            }
            if (gameActionProtocol.Phase == Constants.PlayerScored)
            {
                //int 4*8(bit)=32bit
                var numberBytes = gameActionProtocol.Payload.AsSpan().Slice(gameActionProtocol.PayloadSize - 4, 4);
                return BitConverter.ToInt32(numberBytes.ToArray());
            }
            throw new NotImplementedException($"Not implemented {gameActionProtocol.Phase}");
        }
        public virtual Player GetPlayer(GameActionProtocol gameActionProtocol)
        {
            if (gameActionProtocol.Phase == Constants.NewPlayer || gameActionProtocol.Phase == Constants.KickedPlayer) return GetNewPlayer(ref gameActionProtocol);
            if (gameActionProtocol.Phase == Constants.RegisterPlayer) return GetRegisterePlayer(ref gameActionProtocol);
            if (gameActionProtocol.Phase == Constants.PlayerScored)
            {
                if (gameActionProtocol.Phase != Constants.PlayerScored) throw new ArgumentException($"{nameof(Constants.PlayerScored)} required for argument {nameof(gameActionProtocol.Phase)}");
                var span = gameActionProtocol.Payload.AsSpan();
                Guid guid = new Guid(span.Slice(0, Marshal.SizeOf(typeof(Guid))));
                Player player = new Player("", guid);
                return player;
            }
            throw new NotImplementedException($"Not implemented {gameActionProtocol.Phase}");
        }

        /// <summary>
        /// Gets the <see cref="Player"/> if <see cref="GameActionProtocol.Phase"/> is <see cref="Constants.NewPlayer"/>
        /// </summary>
        /// <param name="gameActionProtocol"></param>
        /// <returns></returns>
        protected virtual Player GetNewPlayer(ref GameActionProtocol gameActionProtocol)
        {
            if (!(gameActionProtocol.Phase == Constants.NewPlayer || gameActionProtocol.Phase == Constants.KickedPlayer)) throw new ArgumentException($"{nameof(Constants.NewPlayer)} required for argument {nameof(gameActionProtocol.Phase)}");
            var span = gameActionProtocol.Payload.AsSpan();

            Guid playerId = new Guid(span.Slice(0, Marshal.SizeOf<Guid>()));

            int playerNameLength = GetPlayerNameLength(gameActionProtocol.PayloadSize, span);
            var playerNameBytes = span.Slice(Marshal.SizeOf(typeof(Guid)), playerNameLength);
            var playername = Encoding.ASCII.GetString(playerNameBytes);

            return new Player(playername.Replace(System.Environment.NewLine, String.Empty), playerId);
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
            Player player = new Player(playername.Replace(System.Environment.NewLine, String.Empty), gameActionProtocol.PlayerId);
            return player;
        }
        public NextRoundEventArgs GetNextRoundEventArgs(GameActionProtocol gameActionPRotocol)
        {
            var span = gameActionPRotocol.Payload.AsSpan();

            var cardNumber = BitConverter.ToInt32(span.Slice(0, 4));
            var round = BitConverter.ToInt32(span.Slice(4, 4));

            return new NextRoundEventArgs(round, new Card(cardNumber));

        }
    }
}
