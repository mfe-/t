using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Text;
using t.lib;
using t.lib.Server;
using Xunit;

namespace t.TestProject1
{
    public class DeAndEncodingTest
    {
        [Fact]
        public void Serializing_and_deserializing_protocol_should_contain_all_information()
        {
            GameActionProtocol expectedGameActionProtocol = new GameActionProtocol();
            expectedGameActionProtocol.Version = Constants.Version;
            expectedGameActionProtocol.PlayerId = Guid.Parse("6d88b7c1-5b8b-4068-b510-b4ff01309670");
            expectedGameActionProtocol.Phase = Constants.RegisterPlayer;
            string playerName = $"martin{Environment.NewLine}";
            expectedGameActionProtocol.Payload = Encoding.ASCII.GetBytes(playerName);
            expectedGameActionProtocol.PayloadSize = (byte)expectedGameActionProtocol.Payload.Length;

            byte[] byteArray = AsByte.ToByteArray(expectedGameActionProtocol);

            GameActionProtocol gameActionProtocol = AsByte.ToGameActionProtocol(byteArray);

            Assert.Equal(expectedGameActionProtocol.Version, gameActionProtocol.Version);
            Assert.Equal(expectedGameActionProtocol.PlayerId, gameActionProtocol.PlayerId);
            Assert.Equal(expectedGameActionProtocol.Phase, gameActionProtocol.Phase);
            Assert.Equal(expectedGameActionProtocol.PayloadSize, gameActionProtocol.PayloadSize);
            Assert.Equal(expectedGameActionProtocol.Payload, gameActionProtocol.Payload);

            Assert.Equal(playerName, Encoding.ASCII.GetString(gameActionProtocol.Payload));


        }
        [Fact]
        public void GameActionProtocol_ToPlayer_test()
        {
            Guid guid = Guid.Parse("6d88b7c1-5b8b-4068-b510-b4ff01309670");
            Player expectedPlayer = new Player("martin", guid);
            GameSocketServer gameSocketServer = new GameSocketServer("", 0, new Mock<ILogger>().Object);
            var gameActionProtocol = gameSocketServer.GameActionProtocolFactory(Constants.NewPlayer, expectedPlayer);

            Player player = gameSocketServer.GetPlayer(gameActionProtocol);

            Assert.Equal(expectedPlayer.PlayerId, player.PlayerId);
            Assert.Equal(expectedPlayer.Name, player.Name);

        }
    }
}
