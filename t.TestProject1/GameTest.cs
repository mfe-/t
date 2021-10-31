using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using t.lib;
using t.lib.Game;
using Xunit;

namespace t.TestProject1
{
    public class GameTest
    {

        [Fact]
        public void player_playing_all_duplicate_cards()
        {
            const int highestPlayedValueCard = 4;

            Mock<GameLogic> gameMock = new Mock<GameLogic>(() => new GameLogic());
            var game = gameMock.Object;
            gameMock.Setup(a => a.MixCards()).Callback(() =>
            {
                game.Cards.Add(new Card(1));
                game.Cards.Add(new Card(2));
                game.Cards.Add(new Card(3));
                game.Cards.Add(new Card(4));
            });
            game.NewGame(4);

            Player player1 = new Player("martin", Guid.NewGuid());
            Player player2 = new Player("simon", Guid.NewGuid());
            Player player3 = new Player("katharina", Guid.NewGuid());
            Player player4 = new Player("renate", Guid.NewGuid());

            game.Players.Add(player2);
            game.Players.Add(player1);
            game.Players.Add(player3);
            game.Players.Add(player4);
            game.PlayerCards.Add(player2, new List<Card>());
            game.PlayerCards.Add(player1, new List<Card>());
            game.PlayerCards.Add(player3, new List<Card>());
            game.PlayerCards.Add(player4, new List<Card>());
            game.Start(10);

            Assert.NotNull(game.CurrentCard);

            game.PlayerReport(player1, new Card(highestPlayedValueCard));
            game.PlayerReport(player2, new Card(highestPlayedValueCard));
            game.PlayerReport(player3, new Card(highestPlayedValueCard));
            game.PlayerReport(player4, new Card(highestPlayedValueCard));
            game.NextRound();
            Assert.True(game.gameActions.All(a => a.RoundFinished == false));

            game.PlayerReport(player1, new Card(3));
            game.PlayerReport(player2, new Card(3));
            game.PlayerReport(player3, new Card(2));
            game.PlayerReport(player4, new Card(2));
            game.NextRound();
            Assert.True(game.gameActions.All(a => a.RoundFinished == false));

            game.PlayerReport(player1, new Card(1));
            game.PlayerReport(player2, new Card(2));
            game.PlayerReport(player3, new Card(5));
            game.PlayerReport(player4, new Card(6));
            game.NextRound();

            var winnerPlayer = game.Players.FirstOrDefault(a => a == player4);
            Assert.NotNull(winnerPlayer);
            Assert.Equal(6, winnerPlayer?.Points);
            Assert.True(game.gameActions.All(a => a.RoundFinished));
        }
        [Fact]
        public void played_pair_card_and_next_highester_card_wins()
        {
            const int highestPlayedValueCard = 4;
            const int secondHighestPlayedValueCard = 3;

            GameLogic game = new GameLogic();
            game.NewGame(4);

            Player player1 = new Player("martin", Guid.NewGuid());
            Player player2 = new Player("simon", Guid.NewGuid());
            Player player3 = new Player("katharina", Guid.NewGuid());
            Player player4 = new Player("renate", Guid.NewGuid());
            Player player5 = new Player("", Guid.NewGuid());

            game.RegisterPlayer(player2);
            game.RegisterPlayer(player1);
            game.RegisterPlayer(player3);
            game.RegisterPlayer(player4);
            game.RegisterPlayer(player5);
            game.Start(10);

            Assert.NotNull(game.CurrentCard);

            int cardToWin = game.CurrentCard?.Value ?? -1;
            game.PlayerReport(player1, new Card(2));
            game.PlayerReport(player2, new Card(highestPlayedValueCard));
            game.PlayerReport(player3, new Card(highestPlayedValueCard));
            game.PlayerReport(player4, new Card(secondHighestPlayedValueCard));
            game.PlayerReport(player5, new Card(secondHighestPlayedValueCard));
            game.NextRound();

            var winnerPlayer = game.Players.FirstOrDefault(a => a == player1);
            Assert.NotNull(winnerPlayer);
            Assert.Equal(cardToWin, winnerPlayer?.Points);
            Assert.True(game.gameActions.All(a => a.RoundFinished));
        }
        [Fact]
        public void played_pair_card_and_higher_single_cards_wins()
        {
            const int highestPlayedValueCard = 3;
            Mock<GameLogic> gameMock = new Mock<GameLogic>(() => new GameLogic());
            var game = gameMock.Object;
            int cardToWin = 7;
            gameMock.Setup(a => a.MixCards()).Callback(() =>
            {
                game.Cards.Add(new Card(cardToWin));
                game.Cards.Add(new Card(2));
            });
            game.NewGame(4);

            Player player1 = new Player("martin", Guid.NewGuid());
            Player player2 = new Player("simon", Guid.NewGuid());
            Player player3 = new Player("katharina", Guid.NewGuid());
            Player player4 = new Player("renate", Guid.NewGuid());

            // add players to list instead of calling game.RegisterPlayer because the Mock doesnt call the original method game.RegisterPlayer
            game.Players.Add(player2);
            game.Players.Add(player1);
            game.Players.Add(player3);
            game.Players.Add(player4);

            game.PlayerCards.Add(player2, new List<Card>());
            game.PlayerCards.Add(player1, new List<Card>());
            game.PlayerCards.Add(player3, new List<Card>());
            game.PlayerCards.Add(player4, new List<Card>());

            game.Start(10);

            game.PlayerReport(player1, new Card(1));
            game.PlayerReport(player2, new Card(2));
            game.PlayerReport(player3, new Card(2));
            game.PlayerReport(player4, new Card(highestPlayedValueCard));

            game.NextRound();

            var winnerPlayer = game.Players.FirstOrDefault(a => a == player4);

            Assert.Equal(cardToWin, winnerPlayer?.Points);
            Assert.True(game.gameActions.All(a => a.RoundFinished));
        }
        [Fact]
        public void player_with_highest_card_wins()
        {
            const int highestPlayedValueCard = 4;
            Mock<GameLogic> gameMock = new Mock<GameLogic>(() => new GameLogic());
            var game = gameMock.Object;
            int cardToWin = 7;
            gameMock.Setup(a => a.MixCards()).Callback(() =>
            {
                game.Cards.Add(new Card(cardToWin));
                game.Cards.Add(new Card(2));
            });
            game.NewGame(4);

            Player player1 = new Player("martin", Guid.NewGuid()); ;
            Player player2 = new Player("simon", Guid.NewGuid());
            Player player3 = new Player("katharina", Guid.NewGuid());
            Player player4 = new Player("renate", Guid.NewGuid());

            //game.RegisterPlayer(player2);
            //game.RegisterPlayer(player1);
            //game.RegisterPlayer(player3);
            //game.RegisterPlayer(player4);
            game.Players.Add(player2);
            game.Players.Add(player1);
            game.Players.Add(player3);
            game.Players.Add(player4);
            game.PlayerCards.Add(player2, new List<Card>());
            game.PlayerCards.Add(player1, new List<Card>());
            game.PlayerCards.Add(player3, new List<Card>());
            game.PlayerCards.Add(player4, new List<Card>());
            game.Start(10);

            game.PlayerReport(player1, new Card(1));
            game.PlayerReport(player2, new Card(2));
            game.PlayerReport(player3, new Card(3));
            game.PlayerReport(player4, new Card(highestPlayedValueCard));

            game.NextRound();

            var winnerPlayer = game.Players.FirstOrDefault(a => a == player4);

            Assert.Equal(cardToWin, winnerPlayer?.Points);
        }
        [Fact]
        public void player_gets_double_points_when_offered_equals_card_value()
        {
            Mock<GameLogic> gameMock = new Mock<GameLogic>(() => new GameLogic());
            var game = gameMock.Object;
            int cardToWin = 7;
            gameMock.Setup(a => a.MixCards()).Callback(() =>
            {
                game.Cards.Add(new Card(cardToWin));
                game.Cards.Add(new Card(2));
            });
            game.NewGame(4);

            Player player1 = new Player("martin", Guid.NewGuid());
            Player player2 = new Player("simon", Guid.NewGuid());
            Player player3 = new Player("katharina", Guid.NewGuid());
            Player player4 = new Player("renate", Guid.NewGuid());


            // add players to list instead of calling game.RegisterPlayer because the Mock doesnt call the original method game.RegisterPlayer
            game.Players.Add(player2);
            game.Players.Add(player1);
            game.Players.Add(player3);
            game.Players.Add(player4);
            game.PlayerCards.Add(player2, new List<Card>());
            game.PlayerCards.Add(player1, new List<Card>());
            game.PlayerCards.Add(player3, new List<Card>());
            game.PlayerCards.Add(player4, new List<Card>());
            game.Start(10);

            game.PlayerReport(player1, new Card(1));
            game.PlayerReport(player2, new Card(2));
            game.PlayerReport(player3, new Card(3));
            game.PlayerReport(player4, new Card(cardToWin));

            game.NextRound();

            var winnerPlayer = game.Players.FirstOrDefault(a => a == player4);

            Assert.Equal(cardToWin * 2, winnerPlayer?.Points);
        }
        [Fact]
        public void Does_remaingin_cards_returned_correctly()
        {
            var game = new GameLogic();
            Player player = new Player("martin", Guid.NewGuid());
            game.NewGame(2);
            game.RegisterPlayer(player);
            var playerCard = game.PlayerCards[player];
            //player should have all 10 cards
            for (int i = 0; i < 10; i++)
            {
                Assert.True(playerCard.Contains(new Card(i + 1)));
            }
            game.Start(10);
            game.PlayerReport(player, new Card(5));

            //player should have all 10 cards except 5
            for (int i = 0; i < 10; i++)
            {
                if (i + 1 == 5)
                {
                    Assert.False(playerCard.Contains(new Card(5)));
                }
                else
                {
                    Assert.True(playerCard.Contains(new Card(i + 1)));
                }
            }
        }
        [Fact]
        public void GetRemainingPlayerForRound_Test()
        {
            var game = new GameLogic();
            game.NewGame(4);

            Player player1 = new Player("martin", Guid.NewGuid()); ;
            Player player2 = new Player("simon", Guid.NewGuid());
            Player player3 = new Player("katharina", Guid.NewGuid());
            Player player4 = new Player("renate", Guid.NewGuid());

            game.RegisterPlayer(player2);
            game.RegisterPlayer(player1);
            game.RegisterPlayer(player3);
            game.RegisterPlayer(player4);

            game.Start(15);

            game.PlayerReport(player1, new Card(5));

            var remainingPlayers = game.GetRemainingPickCardPlayers();


            Assert.Contains(player2, remainingPlayers);
            Assert.Contains(player3, remainingPlayers);
            Assert.Contains(player4, remainingPlayers);

            game.PlayerReport(player2, new Card(5));
            game.PlayerReport(player3, new Card(4));

            remainingPlayers = game.GetRemainingPickCardPlayers();
            Assert.Contains(player4, remainingPlayers);

            game.NextRound();

            remainingPlayers = game.GetRemainingPickCardPlayers();
            Assert.Contains(player1, remainingPlayers);
            Assert.Contains(player2, remainingPlayers);
            Assert.Contains(player3, remainingPlayers);
            Assert.Contains(player4, remainingPlayers);

            game.PlayerReport(player1, new Card(2));


            Assert.Contains(player2, remainingPlayers);
            Assert.Contains(player3, remainingPlayers);
            Assert.Contains(player4, remainingPlayers);

        }
    }
}
