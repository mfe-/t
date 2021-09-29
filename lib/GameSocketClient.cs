using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace t.lib
{
    public class GameSocketClient : GameSocketBase, IDisposable
    {
        private readonly IPAddress serverIpAdress;
        private readonly int serverPort;
        private Socket? _senderSocket;
        internal Player? _player;

        public GameSocketClient(IPAddress serverIpAdress, int serverPort, ILogger logger) : base(logger)
        {
            this.serverIpAdress = serverIpAdress;
            this.serverPort = serverPort;
            ActionDictionary.Add(Constants.NewPlayer, OnNewPlayerAsync);
            ActionDictionary.Add(Constants.NextRound, OnNextRoundAsync);
            ActionDictionary.Add(Constants.PlayerScored, OnPlayerScoredAsync);
        }
        protected virtual ISocket? SenderSocket => _senderSocket;
        private Task OnNewPlayerAsync(GameActionProtocol gameActionProtocol, object? obj)
        {
            if (gameActionProtocol.Phase != Constants.NewPlayer) throw new InvalidOperationException($"Expecting {nameof(gameActionProtocol)} to be in the phase {nameof(Constants.RegisterPlayer)}");

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
                Game.RegisterPlayer(player);
            }
            return Task.CompletedTask;
        }

        // The port number for the remote device.  
        public async Task JoinGameAsync(string name)
        {
            _guid = Guid.NewGuid();
            // Data buffer for incoming data.  
            byte[] bytes = new byte[1024];
            _player = new Player(name, _guid);
            // Connect to a remote device.  

            // Establish the remote endpoint for the socket.  
            // This example uses port 11000 on the local computer.  
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            //IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(serverIpAdress, serverPort);

            // Create a TCP/IP  socket.  
            _senderSocket = new Socket(serverIpAdress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (SenderSocket == null) throw new InvalidOperationException($"{nameof(SenderSocket)} should not be null!");
            // Connect the socket to the remote endpoint. Catch any errors.  
            try
            {
                SenderSocket.Connect(remoteEP);

                _logger.LogInformation("Socket connected to {0}", SenderSocket.RemoteEndPoint?.ToString() ?? $"Could not determine {nameof(_senderSocket.RemoteEndPoint)}");

                GameActionProtocol gameActionProtocol = GameActionProtocolFactory(Constants.RegisterPlayer, _player);
                // Encode the data string into a byte array.  
                byte[] sendPayLoad = gameActionProtocol.ToByteArray();
                // Send the data through the socket.  
                _logger.LogInformation("PlayerId {gamePlayerId} for {playerName} generated", gameActionProtocol.PlayerId, GetPlayer(gameActionProtocol).Name);
                int bytesSent = await SenderSocket.SendAsync(new ArraySegment<byte>(sendPayLoad), SocketFlags.None);
                _logger.LogTrace("Sent {0} bytes to server.", bytesSent);
                _logger.LogInformation("Waiting for remaining players to join");
                //wait until all players joined
                GameActionProtocol gameActionProtocolRec = new GameActionProtocol();
                gameActionProtocolRec.Phase = Constants.Ok;
                while (gameActionProtocolRec.Phase != Constants.StartGame)
                {
                    // Receive the response from the remote device.  
                    bytes = new byte[1024];
                    int bytesRec = SenderSocket.Receive(bytes);
                    _logger.LogTrace("Received {0} bytes.", bytesRec);
                    gameActionProtocolRec = bytes.AsSpan().Slice(0, bytesRec).ToArray().ToGameActionProtocol(bytesRec);
                    await OnMessageReceiveAsync(gameActionProtocolRec, null);
                    //send ok
                    sendPayLoad = GameActionProtocolFactory(Constants.Ok).ToByteArray();
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
                    _logger.LogCritical(se, "Lost connection to {0} SocketException : {1}", serverIpAdress, se.ToString());
                }
                else
                {
                    _logger.LogCritical(se, "Could not connect to {0} SocketException : {1}", serverIpAdress, se.ToString());
                }
                throw;
            }
        }

        public async Task PlayGameAsync(Func<Task<string>> onChoiceCommandFuncAsync, Func<IEnumerable<Card>, Task> showAvailableCardsAsync, Func<NextRoundEventArgs, Task> onNextRoundAsyncFunc)
        {
            if (onChoiceCommandFuncAsync == null) throw new ArgumentNullException(nameof(onChoiceCommandFuncAsync));
            if (showAvailableCardsAsync == null) throw new ArgumentNullException(nameof(showAvailableCardsAsync));
            if (_player == null) throw new InvalidOperationException($"{nameof(_player)} not set!");
            if (SenderSocket == null) throw new InvalidOperationException($"{nameof(_senderSocket)} is not set! Make sure you called {nameof(JoinGameAsync)}");
            if (onNextRoundAsyncFunc == null) throw new ArgumentNullException(nameof(onNextRoundAsyncFunc));

            try
            {
                GameActionProtocol gameActionProtocolRec = new GameActionProtocol();
                GameActionProtocol gameActionProtocolSend = new GameActionProtocol();
                gameActionProtocolRec.Phase = Constants.Ok;
                int bytesSent;
                var bytes = new byte[1024];
                byte[] sendPayLoad = new byte[0];
                while (gameActionProtocolRec.Phase != Constants.PlayerWon)
                {
                    // Receive the response from the remote device.  
                    bytes = new byte[1024];
                    int bytesRec = SenderSocket.Receive(bytes);
                    gameActionProtocolRec = bytes.AsSpan().Slice(0, bytesRec).ToArray().ToGameActionProtocol(bytesRec);
                    // server should send next round or annaounce the winner
                    _logger.LogTrace("Received {0} bytes.", bytesRec);
                    await OnMessageReceiveAsync(gameActionProtocolRec, onNextRoundAsyncFunc);

                    if (gameActionProtocolRec.Phase == Constants.NextRound)
                    {
                        var player = Game.PlayerCards.First(a => a.Key.PlayerId == _guid).Key;
                        //display available cards
                        await showAvailableCardsAsync(Game.PlayerCards[player]);
                        //get picked card
                        int pickedCard = await GetPlayerCardChoiceAsync(onChoiceCommandFuncAsync);
                        //send server picked card
                        gameActionProtocolSend = GameActionProtocolFactory(Constants.PlayerReported, number: pickedCard);
                        sendPayLoad = gameActionProtocolSend.ToByteArray();
                    }
                    else if (gameActionProtocolRec.Phase == Constants.PlayerScored)
                    {
                        //send ok
                        gameActionProtocolSend = GameActionProtocolFactory(Constants.Ok);
                        sendPayLoad = gameActionProtocolSend.ToByteArray();
                    }
                    _logger.LogDebug("Send as {ClientId} {PlayerName} Phase={Phase}", gameActionProtocolSend.PlayerId, _player.Name, Constants.ToString(gameActionProtocolSend.Phase));
                    bytesSent = await SenderSocket.SendAsync(new ArraySegment<byte>(sendPayLoad), SocketFlags.None);
                    _logger.LogTrace("Sent {0} bytes to server.", bytesSent);
                }
            }
            catch (SocketException e)
            {
                _logger.LogCritical(e, e.ToString());
            }
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
                        continue;
                    }
                    break;
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
        protected override Task BroadcastMessageAsync(GameActionProtocol gameActionProtocol, object? obj) => Task.CompletedTask;
        public void ExitGame()
        {
            // Release the socket.  
            SenderSocket?.Shutdown(SocketShutdown.Both);
            SenderSocket?.Close();
            SenderSocket?.Dispose();
            _senderSocket = null;
        }
        public void Dispose()
        {
            ExitGame();
        }
        protected virtual Task OnPlayerScoredAsync(GameActionProtocol gameActionProtocol, object? obj)
        {
            var player = Game.Players.First(a => a.PlayerId == GetPlayer(gameActionProtocol).PlayerId);
            var offeredCardNumber = GetNumber(gameActionProtocol);
            Game.PlayerReport(player, new Card(offeredCardNumber));
            return Task.CompletedTask;
        }
        protected virtual async Task OnNextRoundAsync(GameActionProtocol gameActionProtocol, object? obj)
        {
            if (gameActionProtocol.Phase != Constants.NextRound) throw new InvalidOperationException($"Expecting {nameof(gameActionProtocol)} to be in the phase {nameof(Constants.NextRound)}");
            var nextRoundEventArgs = GetNextRoundEventArgs(gameActionProtocol);
            if (nextRoundEventArgs.Round != 1)
            {
                Game.NextRound();
            }
            //because the Cards are randomly mixed we set the card value from the server manual
            Game.Cards[nextRoundEventArgs.Round - 1].Value = nextRoundEventArgs.Card.Value;
            if (obj is Func<NextRoundEventArgs, Task> nextRoundClientDisplayFunc)
            {
                await nextRoundClientDisplayFunc.Invoke(nextRoundEventArgs);
            }
        }
    }
}