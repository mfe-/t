using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using t.lib;
using Xunit;
using Xunit.Abstractions;

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
        public async Task PlayGameAsync_ask_as_long_user_provides_valid_inputAsync()
        {
            Mock<ILogger> mockLogger = new Mock<ILogger>();
            Mock<GameSocketClient> mockGameSocketClient = new Mock<GameSocketClient>(() => new GameSocketClient(System.Net.IPAddress.Parse("127.0.0.1"), 1, mockLogger.Object));
            Mock<ISocket> mockSocket = new Mock<ISocket>();
            mockSocket.Setup(a => a.Receive(It.IsAny<byte[]>())).Returns((byte[] buffer) =>
            {
                var protocol = mockGameSocketClient.Object.GameActionProtocolFactory(Constants.NextRound, nextRoundEventArgs: new NextRoundEventArgs(1, new Card(4)));
                buffer = protocol.ToByteArray();
                return buffer.Length;
            });

            mockGameSocketClient.Protected().SetupGet<ISocket>("SenderSocket").Returns(mockSocket.Object);
            Player localPlayer = new Player("martin", Guid.NewGuid());

            var game = new GameLogic();
            game.NewGame(2);
            game.RegisterPlayer(localPlayer);

            mockGameSocketClient.Protected().SetupGet<GameLogic>("Game").Returns(game);

            GameSocketClient client = mockGameSocketClient.Object;
        }
    }
}
