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
        [Theory]
        [InlineData("martin", 4)]
        [InlineData("katharina", 6)]
        [InlineData("asdf\ro\n", 6)]
        public void GameActionProtocol_ToPlayer_test(string expectedPlayerName, int expectedRequiredPlayer)
        {
            byte[] byteArray = new byte[] { 4, 5, 6, 13, 0, 10 };
            Guid guid = Guid.Parse("6d88b7c1-5b8b-4068-b510-b4ff01309670");
            Player expectedPlayer = new Player(expectedPlayerName, guid);
            GameSocketServer gameSocketServer = GameSocketFactory();
            var gameActionProtocol = gameSocketServer.GameActionProtocolFactory(Constants.NewPlayer, expectedPlayer, number: expectedRequiredPlayer);

            Player player = gameSocketServer.GetPlayer(gameActionProtocol);
            int requiredPlayer = gameSocketServer.GetNumber(gameActionProtocol);

            Assert.Equal(expectedPlayer.PlayerId, player.PlayerId);
            Assert.Equal(expectedPlayer.Name, player.Name);
            Assert.Equal(expectedRequiredPlayer, requiredPlayer);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public void Serialize_StartGame_And_Deserialize(int totalpoints)
        {
            var gameActionProtocol = GameSocketFactory().GameActionProtocolFactory(Constants.StartGame, number: totalpoints);

            int points = GameSocketFactory().GetTotalPoints(gameActionProtocol);

            Assert.Equal(totalpoints, points);

        }
        private static GameSocketServer GameSocketFactory()
        {
            return new GameSocketServer(new AppConfig() { TotalPoints = 10, RequiredAmountOfPlayers = 2 }, "", 0, new Mock<ILogger>().Object);
        }
    }
}
