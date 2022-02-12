using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using t.lib.Game.EventArgs;
using t.lib.Game;

namespace t.lib.Network
{
    public partial class GameSocketClient : GameSocketBase, IDisposable
    {
        private readonly IPAddress _serverIpAdress;
        private readonly int _serverPort;
        private int? totalGameRounds;
        private Socket? _senderSocket;
        internal Player? _player;

        public GameSocketClient(IPAddress serverIpAdress, int serverPort, ILogger logger) : base(logger)
        {
            this._serverIpAdress = serverIpAdress;
            this._serverPort = serverPort;
            ActionDictionary.Add(PhaseConstants.KickedPlayer, OnPlayerKickedAsync);
            ActionDictionary.Add(PhaseConstants.NewPlayer, OnNewPlayerAsync);
            ActionDictionary.Add(PhaseConstants.NextRound, OnNextRoundAsync);
            ActionDictionary.Add(PhaseConstants.PlayerScored, OnPlayerScoredAsync);
            ActionDictionary.Add(PhaseConstants.PlayerWon, OnPlayerWonAsync);
            ActionDictionary.Add(PhaseConstants.StartGame, OnStartAsync);
        }

        private Task OnPlayerKickedAsync(GameActionProtocol gameActionProtocol, object? arg2)
        {
            Player player = GetPlayer(gameActionProtocol);
            _logger.LogInformation("Player {player} was kicked or left the game", player.Name);

            var removePlayer = Game.Players.FirstOrDefault(a => a.PlayerId == player.PlayerId);
            if (removePlayer != null)
            {
                Game.Players.Remove(removePlayer);
            }

            return Task.CompletedTask;
        }

        protected virtual ISocket? SenderSocket => _senderSocket;
        private Task OnNewPlayerAsync(GameActionProtocol gameActionProtocol, object? obj)
        {
            if (gameActionProtocol.Phase != PhaseConstants.NewPlayer) throw new InvalidOperationException($"Expecting {nameof(gameActionProtocol)} to be in the phase {nameof(PhaseConstants.RegisterPlayer)}");

            Player player = GetPlayer(gameActionProtocol);
            if (!Game.Players.Any(a => a.PlayerId == player.PlayerId))
            {
                if (!Game.Players.Any())
                {
                    //start game before register new players
                    int requiredPlayers = GetNumber(gameActionProtocol);
                    Game.NewGame(requiredPlayers);
                }

                _logger.LogInformation($"Adding {(_guid != player.PlayerId ? "new" : "")} PlayerId {{PlayerId)}} {{Name}}", player.PlayerId, player.Name);
                if (obj is Func<Player, Task> onnewPlayerFunc)
                {
                    onnewPlayerFunc.Invoke(player);
                }
                Game.RegisterPlayer(player);
            }
            return Task.CompletedTask;
        }
        private Task OnStartAsync(GameActionProtocol gameActionProtocol, object? obj)
        {
            var values = GetGameStartValues(gameActionProtocol);

            if (totalGameRounds == null)
            {
                totalGameRounds = values.TotalGameRounds;
                Game.SetFinalRoundsToPlay(totalGameRounds.Value);
            }

            Game.Start(totalPoints: values.Totalpoints);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Returns the current player from the session
        /// </summary>
        /// <remarks>Start a new session with <see cref="JoinGameAsync"/></remarks>
        public Player? Player => _player;
        // The port number for the remote device.  
        public async Task JoinGameAsync(string name, Func<Player, Task>? onPlayerJoinedAsync = null)
        {
            _guid = Guid.NewGuid();
            // Data buffer for incoming data.  
            byte[] bytes = new byte[1024];
            _player = new Player(name, _guid);
            // Connect to a remote device.  

            // Establish the remote endpoint for the socket.  
            IPEndPoint remoteEP = new IPEndPoint(_serverIpAdress, _serverPort);

            // Create a TCP/IP  socket.  
            _senderSocket = new Socket(_serverIpAdress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (SenderSocket == null) throw new InvalidOperationException($"{nameof(SenderSocket)} should not be null!");
            // Connect the socket to the remote endpoint. Catch any errors.  
            try
            {
                SenderSocket.Connect(remoteEP);

                _logger.LogTrace("Socket connected to {0}", SenderSocket.RemoteEndPoint?.ToString() ?? $"Could not determine {nameof(_senderSocket.RemoteEndPoint)}");

                GameActionProtocol gameActionProtocol = GameActionProtocolFactory(PhaseConstants.RegisterPlayer, _player);
                // Encode the data string into a byte array.  
                byte[] sendPayLoad = gameActionProtocol.ToByteArray();
                // Send the data through the socket.  
                _logger.LogTrace("PlayerId {gamePlayerId} for {playerName} generated", gameActionProtocol.PlayerId, GetPlayer(gameActionProtocol).Name);
                int bytesSent = await SenderSocket.SendAsync(new ArraySegment<byte>(sendPayLoad), SocketFlags.None);
                _logger.LogTrace("Sent {0} bytes to server.", bytesSent);
                _logger.LogInformation("Waiting for remaining players to join");
                //wait until all players joined
                GameActionProtocol gameActionProtocolRec = new GameActionProtocol();
                gameActionProtocolRec.Phase = PhaseConstants.Ok;
                while (gameActionProtocolRec.Phase != PhaseConstants.StartGame)
                {
                    // Receive the response from the remote device.  
                    bytes = new byte[1024];
                    int bytesRec = SenderSocket.Receive(bytes);
                    _logger.LogTrace("Received {0} bytes.", bytesRec);
                    gameActionProtocolRec = bytes.AsSpan().Slice(0, bytesRec).ToArray().ToGameActionProtocol(bytesRec);
                    await OnMessageReceiveAsync(gameActionProtocolRec, onPlayerJoinedAsync);
                    //send ok
                    sendPayLoad = GameActionProtocolFactory(PhaseConstants.Ok).ToByteArray();
                    bytesSent = await SenderSocket.SendAsync(new ArraySegment<byte>(sendPayLoad), SocketFlags.None);
                    _logger.LogTrace("Sent {0} bytes to server.", bytesSent);
                }

            }
            catch (ArgumentNullException ane)
            {
                _logger.LogCritical(ane, "ArgumentNullException : {0}", ane.ToString());
                throw;
            }
            catch (SocketException se)
            {
                if (se.ErrorCode == 10054)
                {
                    _logger.LogCritical(se, "Lost connection to {0} SocketException : {1}", _serverIpAdress, se.ToString());
                }
                else
                {
                    _logger.LogCritical(se, "Could not connect to {0} SocketException : {1}", _serverIpAdress, se.ToString());
                }
                throw;
            }
        }

        public async Task PlayGameAsync(MessageReceiveArgs messageReceiveArgs)
        {
            ThrowException(messageReceiveArgs);
            if (_player == null) throw new InvalidOperationException($"{nameof(_player)} not set!");
            if (SenderSocket == null) throw new InvalidOperationException($"{nameof(_senderSocket)} is not set! Make sure you called {nameof(JoinGameAsync)}");
            try
            {
                GameActionProtocol gameActionProtocolRec;
                GameActionProtocol gameActionProtocolSend;
                gameActionProtocolRec.Phase = PhaseConstants.Ok;
                int bytesSent;
                byte[] bytes;
                byte[] sendPayLoad;
                while (gameActionProtocolRec.Phase != PhaseConstants.PlayerWon)
                {
                    // Receive the response from the remote device.  
                    bytes = new byte[1024];
                    int bytesRec = SenderSocket.Receive(bytes);
                    gameActionProtocolRec = bytes.AsSpan().Slice(0, bytesRec).ToArray().ToGameActionProtocol(bytesRec);
                    // server should send next round or annaounce the winner
                    _logger.LogTrace("Received {0} bytes.", bytesRec);

                    await OnMessageReceiveAsync(gameActionProtocolRec, messageReceiveArgs);
                    gameActionProtocolSend = GameActionProtocolFactory(PhaseConstants.Ok);
                    sendPayLoad = gameActionProtocolSend.ToByteArray();
                    if (gameActionProtocolRec.Phase == PhaseConstants.NextRound)
                    {
                        //get picked card
                        int pickedCard = await GetPlayerCardChoiceAsync(messageReceiveArgs.OnChoiceCommandFuncAsync);
                        //send server picked card
                        gameActionProtocolSend = GameActionProtocolFactory(PhaseConstants.PlayerReported, number: pickedCard);
                        sendPayLoad = gameActionProtocolSend.ToByteArray();
                    }
                    else if (gameActionProtocolRec.Phase == PhaseConstants.PlayerScored)
                    {
                        //send ok
                    }
                    _logger.LogDebug("Send as {ClientId} {PlayerName} Phase={Phase}", gameActionProtocolSend.PlayerId, _player.Name, PhaseConstants.ToString(gameActionProtocolSend.Phase));
                    bytesSent = await SenderSocket.SendAsync(new ArraySegment<byte>(sendPayLoad), SocketFlags.None);
                    _logger.LogTrace("Sent {0} bytes to server.", bytesSent);
                }
                SenderSocket.Close();
            }
            catch (SocketException e)
            {
                _logger.LogCritical(e, e.ToString());
            }
        }

        private void ThrowException(MessageReceiveArgs messageReceiveArgs)
        {
            if (messageReceiveArgs == null) throw new ArgumentNullException(nameof(messageReceiveArgs));
        }

        private async Task<int> GetPlayerCardChoiceAsync(Func<Task<string>> onChoiceCommandFuncAsync)
        {
            int cardValue = 0;
            int empty = 0;
            do
            {
                string playerChoice = await onChoiceCommandFuncAsync.Invoke();
                if (int.TryParse(playerChoice, out cardValue))
                {
                    if (!Game.PlayerCards[Game.Players.First(a => a.PlayerId == _player?.PlayerId)].Any(b => b.Value == cardValue))
                    {
                        System.Console.ForegroundColor = ConsoleColor.Red;
                        System.Console.WriteLine("Selected card is not available! Enter a valid card number!");
                        System.Console.ResetColor();
                        cardValue = 0;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    if (empty > 0)
                    {
                        throw new OperationCanceledException("User aborted current operation!");
                    }
                    if (playerChoice == String.Empty) empty++;
                    System.Console.WriteLine("Enter a valid card number!");
                }
            } while (cardValue == 0);

            return cardValue;
        }
        /// <inheritdoc/>
        protected override Task BroadcastMessageAsync(GameActionProtocol gameActionProtocol, object? obj)
            => Task.CompletedTask;
        public void ExitGame()
        {
            try
            {
                // Release the socket.  
                //SenderSocket?.Shutdown(SocketShutdown.Both);
                SenderSocket?.Close();
                SenderSocket?.Dispose();
            }
            catch (ObjectDisposedException)
            {
                //we dont care on dispose exception
            }

            _senderSocket = null;
        }
        protected virtual void Dispose(bool disposing)
        {
            // Cleanup
            ExitGame();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual async Task OnPlayerScoredAsync(GameActionProtocol gameActionProtocol, object? obj)
        {
            var player = Game.Players.First(a => a.PlayerId == GetPlayer(gameActionProtocol).PlayerId);
            var offeredCardNumber = GetNumber(gameActionProtocol);
            Game.PlayerReport(player, new Card(offeredCardNumber));
            if (obj is MessageReceiveArgs messageReceiveArgs)
            {
                await messageReceiveArgs.ShowOfferedByPlayerFunc(player, offeredCardNumber, Game.CurrentCard?.Value ?? 0);
            }
        }
        protected virtual async Task OnNextRoundAsync(GameActionProtocol gameActionProtocol, object? obj)
        {
            if (gameActionProtocol.Phase != PhaseConstants.NextRound) throw new InvalidOperationException($"Expecting {nameof(gameActionProtocol)} to be in the phase {nameof(PhaseConstants.NextRound)}");
            var nextRoundEventArgs = GetNextRoundEventArgs(gameActionProtocol);
            if (nextRoundEventArgs.Round != 1)
            {
                Game.NextRound();
            }
            //because the Cards are randomly mixed we set the card value from the server manual
            Game.Cards[nextRoundEventArgs.Round - 1].Value = nextRoundEventArgs.Card.Value;
            if (obj is MessageReceiveArgs messageReceiveArgs)
            {
                await messageReceiveArgs.OnNextRoundAsyncFunc.Invoke(nextRoundEventArgs);

                //show from last game results
                if (Game.Round != 1)
                {
                    await messageReceiveArgs.ShowPlayerStats(Game.GetPlayerStats());
                }
                var player = Game.PlayerCards.First(a => a.Key.PlayerId == _guid).Key;
                //display available cards
                await messageReceiveArgs.ShowAvailableCardsAsync(Game.PlayerCards[player]);
            }
        }
        private Task OnPlayerWonAsync(GameActionProtocol gameActionProtocol, object? arg2)
        {
            if (arg2 is MessageReceiveArgs messageReceiveArgs)
            {
                //force to calculate current points from last round
                Game.NextRound();
                return messageReceiveArgs.ShowPlayerWonFunc(Game.GetPlayerStats());
            }
            throw new NotImplementedException();
        }
        public static async Task<IEnumerable<PublicGame>> FindLanGamesAsync(int udpPort, CancellationToken cancellationToken)
        {
            //Client uses as receive udp client
            using UdpClient udpClient = new UdpClient(udpPort);
            List<PublicGame> publicGames = new List<PublicGame>();
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {

                    //IPEndPoint object will allow us to read datagrams sent from any source.
                    var receivedResults = await udpClient.ReceiveAsync(cancellationToken);

                    if (TryGetBroadcastMessage(receivedResults.Buffer, out var ipadress, out var port, out var gameName,
                        out var requiredAmountOfPlayers, out var currentAmountPlayers, out var gameRound))
                    {
                        if (!publicGames.Any(a => a.ServerIpAddress != ipadress))
                        {
                            publicGames.Add(new(ipadress, port.Value, requiredAmountOfPlayers.Value, currentAmountPlayers.Value, gameRound.Value, gameName));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                //timeout
            }

            return publicGames;
        }
    }
}