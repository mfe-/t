using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System;
using t.lib;
using t.lib.Game.EventArgs;
using Xunit;
using Xunit.Abstractions;
using t.lib.Game;
using t.lib.Network;

namespace t.TestProject1
{
    public class GameSocketClientTest
    {
        private readonly ITestOutputHelper output;

        public GameSocketClientTest(ITestOutputHelper output)
        {
            this.output = output;
        }
        [Fact]
        public void PlayGameAsync_ask_as_long_user_provides_valid_inputAsync()
        {
            Mock<ILogger> mockLogger = new Mock<ILogger>();
            Mock<GameSocketClient> mockGameSocketClient = new Mock<GameSocketClient>(() => new GameSocketClient(System.Net.IPAddress.Parse("127.0.0.1"), 1, mockLogger.Object));
            Mock<ISocket> mockSocket = new Mock<ISocket>();
            mockSocket.Setup(a => a.Receive(It.IsAny<byte[]>())).Returns((byte[] buffer) =>
            {
                var protocol = mockGameSocketClient.Object.GameActionProtocolFactory(PhaseConstants.NextRound, nextRoundEventArgs: new NextRoundEventArgs(1, new Card(4)));
                buffer = protocol.ToByteArray();
                return buffer.Length;
            });
            //for protected .Setup we need to use ItExp<>
            mockGameSocketClient.Protected().SetupGet<ISocket>("SenderSocket").Returns(mockSocket.Object);
            Player localPlayer = new Player("martin", Guid.NewGuid());

            var game = new GameLogic();
            game.NewGame(2);
            game.RegisterPlayer(localPlayer);

            mockGameSocketClient.Protected().SetupGet<GameLogic>("Game").Returns(game);

            Assert.Equal(1, game.Players.Count);
        }
    }
}
