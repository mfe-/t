using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("t.TestProject1")]
namespace t.lib
{
    public abstract class GameBase
    {
        protected readonly Game _game;
        protected readonly ILogger _logger;
        protected Guid _guid;
        public GameBase(ILogger logger)
        {
            _game = new Game();
            _logger = logger;
            ActionDictionary.Add(Constants.ErrorOccoured, OnProtocolError);

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
            return gameActionProtocol;
        }

        internal virtual Player GetNewPlayer(GameActionProtocol gameActionProtocol)
        {
            if (gameActionProtocol.Phase != Constants.NewPlayer) throw new ArgumentException($"{nameof(Constants.NewPlayer)} required for argument {nameof(gameActionProtocol.Phase)}");
            var span = gameActionProtocol.Payload.AsSpan();

            Guid playerId = new Guid(span.Slice(0, Marshal.SizeOf<Guid>()));
            var playerNameBytes = span.Slice(Marshal.SizeOf(typeof(Guid)), gameActionProtocol.PayloadSize - Marshal.SizeOf(typeof(Guid)));
            var playername = Encoding.ASCII.GetString(playerNameBytes);
            return new Player(playername, playerId);
        }
    }
}
