using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using t.lib.Game.EventArgs;
using t.lib.Game;

[assembly: InternalsVisibleTo("t.TestProject1")]
namespace t.lib.Network
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
            ActionDictionary.Add(PhaseConstants.Ok, OnOkAsync);
            ActionDictionary.Add(PhaseConstants.ErrorOccoured, OnProtocolErrorAsync);
            ActionDictionary.Add(PhaseConstants.RegisterPlayer, OnPlayerRegisterAsync);
        }
        internal virtual GameLogic Game => _game;
        protected virtual Task OnPlayerRegisterAsync(GameActionProtocol gameActionProtocol, object? obj)
        {
            if (gameActionProtocol.Phase != PhaseConstants.RegisterPlayer) throw new InvalidOperationException($"Expecting {nameof(gameActionProtocol)} to be in the phase {nameof(PhaseConstants.RegisterPlayer)}");
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
            _logger.LogTrace("OnMessageReceive from {player} with Phase {Phase}", gameActionProtocol.PlayerId, PhaseConstants.ToString(gameActionProtocol.Phase));
            using (_logger.BeginScope(new Dictionary<string, object> {
                { nameof(gameActionProtocol.Phase), gameActionProtocol.Phase }
               ,{ nameof(gameActionProtocol.PlayerId), gameActionProtocol.PlayerId}
            }))
            {
                if (ActionDictionary.ContainsKey(gameActionProtocol.Phase))
                {
                    await ActionDictionary[gameActionProtocol.Phase].Invoke(gameActionProtocol, obj);
                }
            }
        }
        protected virtual Task OnProtocolErrorAsync(GameActionProtocol gameActionProtocol, object? obj)
        {
            string errormsg = "error message not set";
            try
            {
                if (gameActionProtocol.Payload.Length > 0)
                {
                    errormsg = Encoding.ASCII.GetString(gameActionProtocol.Payload);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, nameof(OnProtocolErrorAsync));
            }
            throw new GameActionProtocolException(gameActionProtocol, errormsg);
        }
        /// <summary>
        /// Broadcasts a <see cref="GameActionProtocol"/> to every recipient
        /// </summary>
        /// <param name="gameActionProtocol">The protocol which should be broadcastet</param>
        protected abstract Task BroadcastMessageAsync(GameActionProtocol gameActionProtocol, object? obj);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual GameActionProtocol GameActionProtocolFactory(byte phase, Player? player = null, string? message = null, int? number = null, int? number2 = null, NextRoundEventArgs? nextRoundEventArgs = null, PlayerWonEventArgs? playerWonEventArgs = null)
        {
            GameActionProtocol gameActionProtocol = new GameActionProtocol();
            gameActionProtocol.Version = PhaseConstants.Version;
            gameActionProtocol.PlayerId = _guid;
            gameActionProtocol.Phase = phase;
            if (gameActionProtocol.Phase == PhaseConstants.NewPlayer || gameActionProtocol.Phase == PhaseConstants.KickedPlayer)
            {
                gameActionProtocol = GameActionProtocolNewPlayer(player, number, ref gameActionProtocol);
            }
            else if (gameActionProtocol.Phase == PhaseConstants.ErrorOccoured)
            {
                if (!String.IsNullOrEmpty(message))
                {
                    var paypload = Encoding.ASCII.GetBytes(message);
                    gameActionProtocol.PayloadSize = (byte)paypload.Length;
                    gameActionProtocol.Payload = paypload;
                }
            }
            else if (gameActionProtocol.Phase == PhaseConstants.RegisterPlayer)
            {
                if (player == null) throw new ArgumentNullException(nameof(player), $"{nameof(PhaseConstants.NewPlayer)} requires argument {nameof(player)}");
                gameActionProtocol.Payload = Encoding.ASCII.GetBytes($"{player.Name}{Seperator}");
            }
            else if (gameActionProtocol.Phase == PhaseConstants.StartGame)
            {
                if (number == null) throw new ArgumentNullException(nameof(number), $"{nameof(PhaseConstants.StartGame)} requires argument {nameof(number)}");
                if (number2 == null) throw new ArgumentNullException(nameof(number2), $"{nameof(PhaseConstants.StartGame)} requires argument {nameof(number2)} for the amount of game rounds which should be played");
                var nbytes = BitConverter.GetBytes(number.Value);
                var n2bytes = BitConverter.GetBytes(number2.Value);
                gameActionProtocol.PayloadSize = (byte)(nbytes.Length + n2bytes.Length);

                Span<byte> numberbytes = stackalloc byte[gameActionProtocol.PayloadSize];
                nbytes.AsSpan().CopyTo(numberbytes);
                n2bytes.AsSpan().CopyTo(numberbytes[4..]);

                gameActionProtocol.Payload = numberbytes.ToArray();

            }
            else if (gameActionProtocol.Phase == PhaseConstants.Ok)
            {
                gameActionProtocol.Payload = new byte[0];
                gameActionProtocol.PayloadSize = (byte)gameActionProtocol.Payload.Length;
            }
            else if (gameActionProtocol.Phase == PhaseConstants.NextRound)
            {
                if (nextRoundEventArgs == null) throw new ArgumentNullException(nameof(nextRoundEventArgs), $"{nameof(PhaseConstants.NextRound)} requries argument {nameof(nextRoundEventArgs)}");
                var cardbytes = BitConverter.GetBytes(nextRoundEventArgs.Card.Value);
                var roundbytes = BitConverter.GetBytes(nextRoundEventArgs.Round);
                byte[] payLoad = new byte[8];
                cardbytes.CopyTo(payLoad, 0);
                roundbytes.CopyTo(payLoad, 4);
                gameActionProtocol.Payload = payLoad;
                gameActionProtocol.PayloadSize = (byte)gameActionProtocol.Payload.Length;
            }
            else if (gameActionProtocol.Phase == PhaseConstants.PlayerReported)
            {
                if (number == null) throw new ArgumentNullException(nameof(number), $"{nameof(PhaseConstants.PlayerReported)} requires argument {nameof(number)}");
                var nbytes = BitConverter.GetBytes(number.Value);
                gameActionProtocol.PayloadSize = (byte)nbytes.Length;
                gameActionProtocol.Payload = nbytes;
            }
            else if (gameActionProtocol.Phase == PhaseConstants.PlayerScored)
            {
                if (number == null) throw new ArgumentNullException(nameof(number), $"{nameof(PhaseConstants.PlayerScored)} requires argument {nameof(number)}");
                if (player == null) throw new ArgumentNullException(nameof(player), $"{nameof(PhaseConstants.PlayerScored)} requires argument {nameof(player)}");
                var playerid = player.PlayerId.ToByteArray();
                var nbytes = BitConverter.GetBytes(number.Value);
                var payload = new byte[nbytes.Length + playerid.Length];
                playerid.CopyTo(payload, 0);
                nbytes.CopyTo(payload, Marshal.SizeOf(typeof(Guid)));
                gameActionProtocol.Payload = payload;
                gameActionProtocol.PayloadSize = (byte)payload.Length;
                return gameActionProtocol;
            }
            else if (gameActionProtocol.Phase == PhaseConstants.PlayerWon)
            {
                if (playerWonEventArgs == null) throw new ArgumentNullException(nameof(playerWonEventArgs), $"{nameof(PhaseConstants.PlayerWon)} requires argument {nameof(playerWonEventArgs)}");
                //calculate the total amount of bytes for all guid players
                Span<byte> totalBytes = stackalloc byte[playerWonEventArgs.Players.Count() * Marshal.SizeOf(typeof(Guid))];
                int iterator = 0;
                foreach (var p in playerWonEventArgs.Players)
                {
                    var a = p.PlayerId.ToByteArray();
                    a.CopyTo<byte>(totalBytes.Slice(iterator));
                    iterator += Marshal.SizeOf(typeof(Guid));
                }
                gameActionProtocol.Payload = totalBytes.ToArray();
                gameActionProtocol.PayloadSize = (byte)gameActionProtocol.Payload.Length;
            }
            else if (gameActionProtocol.Phase == PhaseConstants.WaitingPlayers)
            {
                gameActionProtocol.Payload = new byte[0];
            }
            return gameActionProtocol;
        }
        public static string Seperator = "\r\n";
        private static GameActionProtocol GameActionProtocolNewPlayer(Player? player, int? number, ref GameActionProtocol gameActionProtocol)
        {
            if (player == null) throw new ArgumentNullException(nameof(player), $"{nameof(PhaseConstants.NewPlayer)} requires argument {nameof(player)}");
            if (number == null) throw new ArgumentNullException(nameof(number), $"The parameter {nameof(number)} is required as it is used as {nameof(GameLogic.RequiredAmountOfPlayers)}");
            //encode playerid and playername into payload
            var playerid = player.PlayerId.ToByteArray();
            if (!player.Name.Contains(Seperator))
            {
                player.Name = $"{player.Name}{Seperator}";
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

        internal (int Totalpoints, int TotalGameRounds) GetGameStartValues(GameActionProtocol gameActionProtocol)
        {
            if (gameActionProtocol.Phase != PhaseConstants.StartGame) throw new ArgumentException($"{nameof(PhaseConstants.StartGame)} required for argument {nameof(gameActionProtocol.Phase)}");

            var p = BitConverter.ToInt32(gameActionProtocol.Payload.AsSpan().Slice(0, 4));
            var r = BitConverter.ToInt32(gameActionProtocol.Payload.AsSpan().Slice(4));
            return (p, r);
        }
        internal int GetNumber(GameActionProtocol gameActionProtocol)
        {
            if (gameActionProtocol.Phase == PhaseConstants.NewPlayer)
            {
                var span = gameActionProtocol.Payload.AsSpan();

                int playerNameLength = GetPlayerNameLength(gameActionProtocol.PayloadSize, span);
                //slice from guid+playername position to the end of byte array
                var numberBytes = span.Slice(Marshal.SizeOf(typeof(Guid)) + playerNameLength, gameActionProtocol.PayloadSize - (Marshal.SizeOf(typeof(Guid)) + playerNameLength));
                return BitConverter.ToInt32(numberBytes.ToArray());
            }
            if (gameActionProtocol.Phase == PhaseConstants.StartGame || gameActionProtocol.Phase == PhaseConstants.PlayerReported)
            {
                var span = gameActionProtocol.Payload.AsSpan();
                return BitConverter.ToInt32(span);
            }
            if (gameActionProtocol.Phase == PhaseConstants.PlayerScored)
            {
                //int 4*8(bit)=32bit
                var numberBytes = gameActionProtocol.Payload.AsSpan().Slice(gameActionProtocol.PayloadSize - 4, 4);
                return BitConverter.ToInt32(numberBytes.ToArray());
            }
            throw new NotImplementedException($"Not implemented {gameActionProtocol.Phase}");
        }
        public virtual Player GetPlayer(GameActionProtocol gameActionProtocol)
        {
            if (gameActionProtocol.Phase == PhaseConstants.NewPlayer || gameActionProtocol.Phase == PhaseConstants.KickedPlayer) return GetNewPlayer(ref gameActionProtocol);
            if (gameActionProtocol.Phase == PhaseConstants.RegisterPlayer) return GetRegisterePlayer(ref gameActionProtocol);
            if (gameActionProtocol.Phase == PhaseConstants.PlayerScored)
            {
                if (gameActionProtocol.Phase != PhaseConstants.PlayerScored) throw new ArgumentException($"{nameof(PhaseConstants.PlayerScored)} required for argument {nameof(gameActionProtocol.Phase)}");
                var span = gameActionProtocol.Payload.AsSpan();
                Guid guid = new Guid(span.Slice(0, Marshal.SizeOf(typeof(Guid))));
                Player player = new Player("", guid);
                return player;
            }
            throw new NotImplementedException($"Not implemented {gameActionProtocol.Phase}");
        }

        public IEnumerable<Player> GetPlayers(GameActionProtocol gameActionProtocol)
        {
            if (gameActionProtocol.Phase != PhaseConstants.PlayerWon) throw new ArgumentException($"{nameof(gameActionProtocol)} requires property {nameof(gameActionProtocol.Phase)} to be set to {nameof(PhaseConstants.PlayerWon)}");

            int amountGuids = gameActionProtocol.PayloadSize / Marshal.SizeOf(typeof(Guid));
            var payload = gameActionProtocol.Payload.AsSpan();
            Player[] players = new Player[amountGuids];
            int a = 0;
            for (int iterator = 0; iterator < amountGuids * Marshal.SizeOf(typeof(Guid)); iterator += Marshal.SizeOf(typeof(Guid)))
            {
                Guid g = new Guid(payload.Slice(iterator, Marshal.SizeOf(typeof(Guid))));
                players[a] = new Player(String.Empty, g);
                a++;
            }

            return players;
        }

        /// <summary>
        /// Gets the <see cref="Player"/> if <see cref="GameActionProtocol.Phase"/> is <see cref="PhaseConstants.NewPlayer"/>
        /// </summary>
        /// <param name="gameActionProtocol"></param>
        /// <returns></returns>
        protected virtual Player GetNewPlayer(ref GameActionProtocol gameActionProtocol)
        {
            if (!(gameActionProtocol.Phase == PhaseConstants.NewPlayer || gameActionProtocol.Phase == PhaseConstants.KickedPlayer)) throw new ArgumentException($"{nameof(PhaseConstants.NewPlayer)} required for argument {nameof(gameActionProtocol.Phase)}");
            var span = gameActionProtocol.Payload.AsSpan();

            Guid playerId = new Guid(span.Slice(0, Marshal.SizeOf<Guid>()));

            int playerNameLength = GetPlayerNameLength(gameActionProtocol.PayloadSize, span);
            var playerNameBytes = span.Slice(Marshal.SizeOf(typeof(Guid)), playerNameLength);
            var playername = Encoding.ASCII.GetString(playerNameBytes);

            return new Player(playername.Replace(Seperator, String.Empty), playerId);
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
        /// Gets the <see cref="Player"/> if <see cref="GameActionProtocol.Phase"/> is <see cref="PhaseConstants.RegisterPlayer"/>
        /// </summary>
        /// <param name="gameActionProtocol"></param>
        /// <returns></returns>
        protected Player GetRegisterePlayer(ref GameActionProtocol gameActionProtocol)
        {
            if (gameActionProtocol.Phase != PhaseConstants.RegisterPlayer) throw new ArgumentException($"{nameof(PhaseConstants.RegisterPlayer)} required for argument {nameof(gameActionProtocol.Phase)}");
            string playername = Encoding.ASCII.GetString(gameActionProtocol.Payload).Replace(Seperator, String.Empty);
            Player player = new Player(playername.Replace(Seperator, String.Empty), gameActionProtocol.PlayerId);
            return player;
        }
        public NextRoundEventArgs GetNextRoundEventArgs(GameActionProtocol gameActionPRotocol)
        {
            var span = gameActionPRotocol.Payload.AsSpan();

            var cardNumber = BitConverter.ToInt32(span.Slice(0, 4));
            var round = BitConverter.ToInt32(span.Slice(4, 4));

            return new NextRoundEventArgs(round, new Card(cardNumber));

        }
        internal static bool TryGetBroadcastMessage(byte[] broadcastmsg, [NotNullWhen(true)] out IPAddress? iPAddress, [NotNullWhen(true)] out int? port, [NotNullWhen(true)] out string? servername, [NotNullWhen(true)] out int? requiredAmountOfPlayers, [NotNullWhen(true)] out int? currentAmountOfPlayers, [NotNullWhen(true)] out int? gameRounds)
        {
            iPAddress = null;
            port = null;
            servername = null;
            requiredAmountOfPlayers = null;
            gameRounds = null;
            currentAmountOfPlayers = null;
            try
            {
                var msg = broadcastmsg.AsSpan();
                iPAddress = new IPAddress(msg.Slice(0, 4));
                port = BitConverter.ToInt32(msg.Slice(4, 4).ToArray());
                requiredAmountOfPlayers = BitConverter.ToInt32(msg.Slice(8, 4).ToArray());
                gameRounds = BitConverter.ToInt32(msg.Slice(12, 4).ToArray());
                currentAmountOfPlayers = BitConverter.ToInt32(msg.Slice(16,4).ToArray());
                servername = Encoding.ASCII.GetString(msg.Slice(20, msg.Length - 20));
                servername = servername.Replace("\0", String.Empty).TrimEnd();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
    }
}
