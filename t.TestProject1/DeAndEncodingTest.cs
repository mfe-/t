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
        [Theory]
        [InlineData(1,4)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(int.MinValue, int.MinValue)]
        [InlineData(int.MinValue, int.MaxValue)]
        public void Serialize_NextRound_And_Deserialze(int expectedRound,int expectedCardnumber)
        {
            var gameActionProtocol = GameSocketFactory().GameActionProtocolFactory(Constants.NextRound, nextRoundEventArgs: new NextRoundEventArgs(expectedRound, new Card(expectedCardnumber)));
            var nextRoundEventArgs = GameSocketFactory().GetNextRoundEventArgs(gameActionProtocol);

            Assert.Equal(expectedRound,nextRoundEventArgs.Round);
            Assert.Equal(expectedCardnumber, nextRoundEventArgs.Card.Value);
        }
        [Theory]
        [InlineData(4)]
        public void Serialize_PlayerReport_AndDeserialze(int expectedPickedCard)
        {
            var gameActionProtocol = GameSocketFactory().GameActionProtocolFactory(Constants.PlayerReported, number: expectedPickedCard);
            var pickedCard = GameSocketFactory().GetNumber(gameActionProtocol);

            Assert.Equal(expectedPickedCard,pickedCard);
        }
        [Fact]
        public void Serialize_PlayerScroed_AndDeserialze()
        {
            Player expectedPlayer = new Player("martin", Guid.NewGuid());
            int offeredExpected = 5;
            var gameActionProtocol = GameSocketFactory().GameActionProtocolFactory(Constants.PlayerScored, player: expectedPlayer, number: offeredExpected);
            int offered = GameSocketFactory().GetNumber(gameActionProtocol);
            Assert.Equal(offeredExpected, offered);
            var player = GameSocketFactory().GetPlayer(gameActionProtocol);

            Assert.Equal(player.PlayerId, player.PlayerId);
        }

        private static GameSocketServer GameSocketFactory()
        {
            return new GameSocketServer(new AppConfig() { TotalPoints = 10, RequiredAmountOfPlayers = 2 }, "", 0, new Mock<ILogger>().Object);
        }
    }
}
