﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace t.lib.Server
{
    public partial class GameSocketServer : GameBase, IHostedService
    {

        public GameSocketServer(string serverIpAdress, int serverPort, ILogger logger) : base(logger)
        {
            ServerPort = serverPort;
            ServerIpAdress = serverIpAdress;
            _game.NewPlayerRegisteredEvent += Game_NewPlayerRegisteredEvent;
            _guid = Guid.NewGuid();
            ActionDictionary.Add(Constants.RegisterPlayer, OnPlayerRegister);
        }
        public GameSocketServer(string serverIpAdress, int serverPort, ILogger logger, Guid guid)
            : this(serverIpAdress, serverPort, logger)
        {
            _guid = guid;
        }
        protected virtual void OnPlayerRegister(GameActionProtocol gameActionProtocol)
        {
            if (gameActionProtocol.Phase != Constants.RegisterPlayer) throw new InvalidOperationException($"Expecting {nameof(gameActionProtocol)} to be in the phase {nameof(Constants.RegisterPlayer)}");

            Player player = GetPlayer(gameActionProtocol);
            _game.RegisterPlayer(player);
        }
        private void Game_NewPlayerRegisteredEvent(object? sender, EventArgs<Player> e)
        {
            //broadcast the new player to all other players
            GameActionProtocol gameActionProtocol = GameActionProtocolFactory(Constants.NewPlayer, e.Data);
            BroadcastMessage(gameActionProtocol);
        }
        public int ServerPort { get; }
        public string ServerIpAdress { get; }

        private ConcurrentDictionary<Guid, ClientConnectionState> _playerConnections = new ConcurrentDictionary<Guid, ClientConnectionState>();

        private async Task StartListeningAsync(CancellationToken cancellationToken)
        {
            // Establish the local endpoint for the socket.  
            // Dns.GetHostName returns the name of the
            // host running the application.  
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress;
            if (String.IsNullOrEmpty(ServerIpAdress))
            {
                ipAddress = ipHostInfo.AddressList[0];
            }
            else
            {
                ipAddress = IPAddress.Parse(ServerIpAdress);
            }
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP socket.  
            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            await Task.Factory.StartNew(() => Listening(localEndPoint, listener),
                cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

        }
        private CancellationTokenSource? cancellationTokenSource;
        // Thread signal.  
        protected readonly ManualResetEvent allDone = new ManualResetEvent(false);
        private void Listening(IPEndPoint localEndPoint, Socket listener)
        {
            _logger.LogInformation($"{nameof(Listening)} on {localEndPoint.Address} {ServerPort}", localEndPoint.Address, ServerPort);
            // Bind the socket to the local endpoint and
            // listen for incoming connections.  
            using (cancellationTokenSource = new CancellationTokenSource())
            {
                try
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(10);

                    _game.NewGame();

                    // Start listening for connections.  
                    while (true)
                    {
                        if (cancellationTokenSource != null && cancellationTokenSource.IsCancellationRequested)
                            break;
                        // Set the event to nonsignaled state.  
                        allDone.Reset();

                        // Start an asynchronous socket to listen for connections.  
                        _logger.LogInformation("Waiting for a connection...");
                        listener.BeginAccept(
                            new AsyncCallback(AcceptClientConnection),
                            listener);

                        // Wait until a connection is made before continuing.  
                        allDone.WaitOne();
                    }

                    //while (true)
                    //{
                    //    _logger.LogInformation("Waiting for a connection...");
                    //    // Program is suspended while waiting for an incoming connection.  
                    //    Socket handler = listener.Accept();
                    //    // An incoming connection needs to be processed.  
                    //    while (true)
                    //    {
                    //        //check if a cancellation was requested
                    //        int bytesRec = handler.Receive(bytes);


                    //    }

                    //    // Show the data on the console.  


                    //    // Echo the data back to the client.  


                    //    //handler.Send(msg);
                    //    handler.Shutdown(SocketShutdown.Both);
                    //    handler.Close();
                    //}
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "exception while listing");
                }
            }
            cancellationTokenSource = null;
        }

        private void AcceptClientConnection(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            // Get the socket that handles the client request.  
            if (ar.AsyncState is Socket socket)
            {
                Socket listener = socket;
                Socket handler = listener.EndAccept(ar);

                // Create the state object.  
                ClientConnectionState state = new ClientConnectionState(handler);
                handler.BeginReceive(state.Buffer, 0, ClientConnectionState.BufferSize, 0, new AsyncCallback(ReadResult), state);
            }
        }

        private void ReadResult(IAsyncResult ar)
        {
            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            if (ar.AsyncState is ClientConnectionState connectionState)
            {
                ClientConnectionState state = connectionState;
                Socket handler = state.workSocket;

                // Read data from the client socket.
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    _logger.LogTrace("received : {0}", bytesRead);

                    GameActionProtocol gameActionProtocol = state.Buffer.ToGameActionProtocol(bytesRead);
                    if (gameActionProtocol.Phase == Constants.RegisterPlayer && !_playerConnections.ContainsKey(gameActionProtocol.PlayerId))
                    {
                        connectionState.Player = GetPlayer(gameActionProtocol);
                        var addResult = _playerConnections.TryAdd(gameActionProtocol.PlayerId, connectionState);
                        _logger.LogInformation("Client {connectionWithPlayerId} {PlayerName} connected and added to Clientlist={addResult}", gameActionProtocol.PlayerId, connectionState.Player?.Name ?? "", addResult);
                    }
                    OnMessageReceive(gameActionProtocol);
                }
            }
            else
            {
                _logger.LogInformation("Recieved something unknown");
            }
        }
        protected void Send(Socket handler, GameActionProtocol gameActionProtocol)
        {
            // Convert the data to byte data
            byte[] byteData = gameActionProtocol.ToByteArray();

            // Begin sending the data to the remote device.  
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                if (ar.AsyncState is Socket)
                {
                    Socket handler = (Socket)ar.AsyncState;

                    // Complete sending the data to the remote device.  
                    int bytesSent = handler.EndSend(ar);
                    _logger.LogInformation("Sent {0} bytes to client.", bytesSent);

                    //handler.Shutdown(SocketShutdown.Both);
                    //handler.Close();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, nameof(SendCallback));
            }
        }

        private void StopListening()
        {
            try
            {
                cancellationTokenSource?.Cancel();
            }
            catch (ObjectDisposedException)
            {

            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return StartListeningAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            StopListening();
            return Task.CompletedTask;
        }

        protected override void BroadcastMessage(GameActionProtocol gameActionProtocol)
        {
            foreach (var connectionState in _playerConnections.Values)
            {
                _logger.LogInformation("Broadcasting {Phase} to {PlayerName} {PlayerId}", gameActionProtocol.Phase, connectionState.Player?.Name ?? "", gameActionProtocol.PlayerId);
                Send(connectionState.workSocket, gameActionProtocol);
            }
        }
    }
}