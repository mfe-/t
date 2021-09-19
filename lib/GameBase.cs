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
            ActionDictionary.Add(Constants.ErrorOccoured, OnProtocolError);
            ActionDictionary.Add(Constants.RegisterPlayer, OnPlayerRegister);
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

        protected Dictionary<byte, Action<GameActionProtocol>> ActionDictionary = new();

        protected virtual void OnMessageReceive(GameActionProtocol gameActionProtocol)
        {
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

        internal virtual GameActionProtocol GameActionProtocolFactory(byte phase, Player? player = null, string? message = null)
        {
            GameActionProtocol gameActionProtocol = new GameActionProtocol();
            gameActionProtocol.Version = Constants.Version;
            gameActionProtocol.PlayerId = _guid;
            gameActionProtocol.Phase = phase;
            if (gameActionProtocol.Phase == Constants.NewPlayer)
            {
                if (player == null) throw new ArgumentException($"{nameof(Constants.NewPlayer)} requires argument {nameof(player)}");
                //encode playerid and playername into payload
                var playerid = player.PlayerId.ToByteArray();
                if (!player.Name.Contains(Environment.NewLine))
                {
                    player.Name = $"{player.Name}{Environment.NewLine}";
                }
                var playername = Encoding.ASCII.GetBytes(player.Name);
                byte[] paypload = new byte[playerid.Length + playername.Length];
                int i = 0;
                for (; i < playerid.Length; i++)
                {
                    paypload[i] = playerid[i];
                }
                for (int a = 0; a < playername.Length; i++, a++)
                {
                    paypload[i] = playername[a];
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
                if (player == null) throw new ArgumentException($"{nameof(Constants.NewPlayer)} requires argument {nameof(player)}");
                gameActionProtocol.Payload = Encoding.ASCII.GetBytes($"{player.Name}{Environment.NewLine}");
            }
            return gameActionProtocol;
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
            var playerNameBytes = span.Slice(Marshal.SizeOf(typeof(Guid)), gameActionProtocol.PayloadSize - Marshal.SizeOf(typeof(Guid)));
            var playername = Encoding.ASCII.GetString(playerNameBytes);
            return new Player(playername, playerId);
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
