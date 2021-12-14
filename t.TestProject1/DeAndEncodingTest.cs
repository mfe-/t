using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Text;
using t.lib;
using t.lib.Game.EventArgs;
using t.lib.Server;
using t.lib.Game;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using t.lib.Network;

namespace t.TestProject1
{
    public class DeAndEncodingTest
    {
        [Fact]
        public void Serializing_and_deserializing_protocol_should_contain_all_information()
        {
            GameActionProtocol expectedGameActionProtocol = new GameActionProtocol();
            expectedGameActionProtocol.Version = PhaseConstants.Version;
            expectedGameActionProtocol.PlayerId = Guid.Parse("6d88b7c1-5b8b-4068-b510-b4ff01309670");
            expectedGameActionProtocol.Phase = PhaseConstants.RegisterPlayer;
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
            Guid guid = Guid.Parse("6d88b7c1-5b8b-4068-b510-b4ff01309670");
            Player expectedPlayer = new Player(expectedPlayerName, guid);
            GameSocketServer gameSocketServer = GameSocketFactory();
            var gameActionProtocol = gameSocketServer.GameActionProtocolFactory(PhaseConstants.NewPlayer, expectedPlayer, number: expectedRequiredPlayer);

            Player player = gameSocketServer.GetPlayer(gameActionProtocol);
            int requiredPlayer = gameSocketServer.GetNumber(gameActionProtocol);

            Assert.Equal(expectedPlayer.PlayerId, player.PlayerId);
            Assert.Equal(expectedPlayer.Name.Replace(Environment.NewLine, String.Empty), player.Name);
            Assert.Equal(expectedRequiredPlayer, requiredPlayer);
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(10, 3)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(int.MinValue, int.MaxValue)]
        public void Serialize_StartGame_And_Deserialize(int totalpoints, int totalgamerounds)
        {
            var gameActionProtocol = GameSocketFactory().GameActionProtocolFactory(PhaseConstants.StartGame,
                number: totalpoints, number2: totalgamerounds);

            var values = GameSocketFactory().GetGameStartValues(gameActionProtocol);

            Assert.Equal(totalpoints, values.Totalpoints);
            Assert.Equal(totalgamerounds, values.TotalGameRounds);

        }
        [Theory]
        [InlineData(1, 4)]
        [InlineData(int.MaxValue, int.MaxValue)]
        [InlineData(int.MinValue, int.MinValue)]
        [InlineData(int.MinValue, int.MaxValue)]
        public void Serialize_NextRound_And_Deserialze(int expectedRound, int expectedCardnumber)
        {
            var gameActionProtocol = GameSocketFactory().GameActionProtocolFactory(PhaseConstants.NextRound, nextRoundEventArgs: new NextRoundEventArgs(expectedRound, new Card(expectedCardnumber)));
            var nextRoundEventArgs = GameSocketFactory().GetNextRoundEventArgs(gameActionProtocol);

            Assert.Equal(expectedRound, nextRoundEventArgs.Round);
            Assert.Equal(expectedCardnumber, nextRoundEventArgs.Card.Value);
        }
        [Theory]
        [InlineData(4)]
        public void Serialize_PlayerReport_AndDeserialze(int expectedPickedCard)
        {
            var gameActionProtocol = GameSocketFactory().GameActionProtocolFactory(PhaseConstants.PlayerReported, number: expectedPickedCard);
            var pickedCard = GameSocketFactory().GetNumber(gameActionProtocol);

            Assert.Equal(expectedPickedCard, pickedCard);
        }
        [Fact]
        public void Serialize_PlayerScroed_AndDeserialze()
        {
            Player expectedPlayer = new Player("martin", Guid.NewGuid());
            int offeredExpected = 5;
            var gameActionProtocol = GameSocketFactory().GameActionProtocolFactory(PhaseConstants.PlayerScored, player: expectedPlayer, number: offeredExpected);
            int offered = GameSocketFactory().GetNumber(gameActionProtocol);
            Assert.Equal(offeredExpected, offered);
            var player = GameSocketFactory().GetPlayer(gameActionProtocol);

            Assert.Equal(player.PlayerId, player.PlayerId);
        }
        [Fact]
        public void Serialize_PlayerWonEventArgs_()
        {
            var player1 = Guid.Parse("24397370-d5b7-468d-815f-e1599502caeb");
            var player2 = Guid.Parse("2fc77b7b-8c14-4451-b603-8ab3a9060a4f");

            Player expectedPlayer = new Player("martin", player1);
            Player expectedPlayer1 = new Player("stefan", player2);

            var playerWonEventArgs = new PlayerWonEventArgs(new List<Player>() { expectedPlayer, expectedPlayer1 });

            var gameActionProtocol = GameSocketFactory().GameActionProtocolFactory(PhaseConstants.PlayerWon, playerWonEventArgs: playerWonEventArgs);

            var players = GameSocketFactory().GetPlayers(gameActionProtocol);


            Assert.Equal(player1, players.First(a => a.PlayerId == player1).PlayerId);
            Assert.Equal(player2, players.First(a => a.PlayerId == player2).PlayerId);

        }
        [Theory]
        [InlineData("10.51.51.195", 11000, "kathisserver", 4, 1, 2)]
        [InlineData("192.168.0.192", 99000, "martins cooler host", 6, 1, 1)]
        public void GenerateBroadcastMessage_and_decode(string ipadress, int port, string servername, int requiredAmountOfPlayers, int currentAmountOfPlayers, int gameRounds)
        {
            GameSocketServer gameSocketServer = GameSocketFactory(ipadress, port);
            var msgBytes = gameSocketServer.GenerateBroadcastMessage(IPAddress.Parse(ipadress), port, servername, requiredAmountOfPlayers, currentAmountOfPlayers, gameRounds);

            GameSocketServer.TryGetBroadcastMessage(msgBytes, out var ip, out var p, out var name, out var amountPlayers, out var currentPlayerAmount, out var rounds);

            Assert.Equal(ipadress, ip?.ToString());
            Assert.Equal(port, p);
            Assert.Equal(servername, name);
            Assert.Equal(requiredAmountOfPlayers, amountPlayers);
            Assert.Equal(currentAmountOfPlayers, currentPlayerAmount);
            Assert.Equal(gameRounds, rounds);
        }

        private static GameSocketServer GameSocketFactory(string ipadress = "", int port = 0)
        {
            return new GameSocketServer(new AppConfig() { TotalPoints = 10, RequiredAmountOfPlayers = 2, GameRounds = 2 }, "", 0, 0, new Mock<ILogger>().Object);
        }
    }
}
