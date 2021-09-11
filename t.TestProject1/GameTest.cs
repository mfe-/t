using Moq;
using Moq.Protected;
using System.Linq;
using t.lib;
using Xunit;

namespace t.TestProject1
{
    public class GameTest
    {

        [Fact]
        public void player_playing_all_duplicate_cards()
        {
            const int highestPlayedValueCard = 4;

            Mock<Game> gameMock = new Mock<Game>(() => new Game());
            var game = gameMock.Object;
            gameMock.Setup(a => a.MixCards()).Callback(() =>
            {
                game.Cards.Add(new Card(1));
                game.Cards.Add(new Card(2));
                game.Cards.Add(new Card(3));
                game.Cards.Add(new Card(4));
            });
            game.NewGame();

            Player player1 = new Player("martin");
            Player player2 = new Player("simon");
            Player player3 = new Player("katharina");
            Player player4 = new Player("renate");

            game.RegisterPlayer(player2);
            game.RegisterPlayer(player1);
            game.RegisterPlayer(player3);
            game.RegisterPlayer(player4);
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

            Game game = new Game();
            game.NewGame();

            Player player1 = new Player("martin");
            Player player2 = new Player("simon");
            Player player3 = new Player("katharina");
            Player player4 = new Player("renate");
            Player player5 = new Player("");

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
            Mock<Game> gameMock = new Mock<Game>(() => new Game());
            var game = gameMock.Object;
            int cardToWin = 7;
            gameMock.Setup(a => a.MixCards()).Callback(() =>
            {
                game.Cards.Add(new Card(cardToWin));
                game.Cards.Add(new Card(2));
            });
            game.NewGame();

            Player player1 = new Player("martin");
            Player player2 = new Player("simon");
            Player player3 = new Player("katharina");
            Player player4 = new Player("renate");

            game.RegisterPlayer(player2);
            game.RegisterPlayer(player1);
            game.RegisterPlayer(player3);
            game.RegisterPlayer(player4);

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
            Mock<Game> gameMock = new Mock<Game>(() => new Game());
            var game = gameMock.Object;
            int cardToWin = 7;
            gameMock.Setup(a => a.MixCards()).Callback(() =>
            {
                game.Cards.Add(new Card(cardToWin));
                game.Cards.Add(new Card(2));
            });
            game.NewGame();

            Player player1 = new Player("martin");
            Player player2 = new Player("simon");
            Player player3 = new Player("katharina");
            Player player4 = new Player("renate");

            game.RegisterPlayer(player2);
            game.RegisterPlayer(player1);
            game.RegisterPlayer(player3);
            game.RegisterPlayer(player4);

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
            Mock<Game> gameMock = new Mock<Game>(() => new Game());
            var game = gameMock.Object;
            int cardToWin = 7;
            gameMock.Setup(a => a.MixCards()).Callback(() =>
            {
                game.Cards.Add(new Card(cardToWin));
                game.Cards.Add(new Card(2));
            });
            game.NewGame();

            Player player1 = new Player("martin");
            Player player2 = new Player("simon");
            Player player3 = new Player("katharina");
            Player player4 = new Player("renate");

            game.RegisterPlayer(player2);
            game.RegisterPlayer(player1);
            game.RegisterPlayer(player3);
            game.RegisterPlayer(player4);

            game.Start(10);

            game.PlayerReport(player1, new Card(1));
            game.PlayerReport(player2, new Card(2));
            game.PlayerReport(player3, new Card(3));
            game.PlayerReport(player4, new Card(cardToWin));

            game.NextRound();

            var winnerPlayer = game.Players.FirstOrDefault(a => a == player4);

            Assert.Equal(cardToWin * 2, winnerPlayer?.Points);
        }
    }
}
