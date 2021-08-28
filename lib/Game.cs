using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace t.lib
{
    public class Game
    {
        private const int CardCapacity = 10;
        private const string InitializeAndStartNewGameMessage = $"Start a new Game with {nameof(NewGame)} and {nameof(Start)}";
        private readonly Random random;
        private readonly Stack<GameAction> gameActions = new Stack<GameAction>();
        private event EventHandler<EventArgs>? GameStartedEvent;
        private event EventHandler<EventArgs>? NextRoundEvent;
        public Game()
        {
            random = new Random();
            Cards = new List<Card>(CardCapacity);
            Players = new List<Player>();
        }
        public IList<Player> Players { get; private set; }

        public IList<Card> Cards { get; private set; }

        public int Round { get; private set; }

        public Card? CurrentCard { get; private set; }

        public void RegisterPlayer(Player player)
        {
            if (player == null) throw new ArgumentNullException();
            if (Cards.Count == 0) throw new InvalidOperationException(InitializeAndStartNewGameMessage);
            if (Players.Count == CardCapacity) throw new InvalidOperationException($"Game only designed for {CardCapacity} players");
            Players.Add(player);
        }
        public void OnLeavePlayerEvent(Player player)
        {
            Players.Remove(player);
        }
        public void MixCards()
        {
            Cards.Clear();
            do
            {
                int val = random.Next(1, CardCapacity + 1);
                if (!Cards.Any(a => a.Value == val))
                {
                    Cards.Add(new Card(val));
                }
            }
            while (Cards.Count != CardCapacity);
        }

        public void NewGame()
        {
            Round = 0;
            CurrentCard = null;
            Players.Clear();
            gameActions.Clear();
            MixCards();
        }
        public void Start()
        {
            OnStartEvent(EventArgs.Empty);
            NextRound();
        }
        private void OnStartEvent(EventArgs e)
        {
            GameStartedEvent?.Invoke(this, e);
        }
        public void NextRound()
        {
            OnNextRoundEvent(EventArgs.Empty);
            if (Round != 0)
            {
                EvaluateWinner();
            }
            Round++;
            CurrentCard = Cards[Round - 1];
        }

        private void EvaluateWinner()
        {
            if (CurrentCard == null) throw new InvalidOperationException(InitializeAndStartNewGameMessage);
            var currentRoundAction = gameActions.Where(a => a.Round == Round);
            var maxOffer = currentRoundAction.Max(b => b.Offered);
            Player winner = currentRoundAction.First(a => a.Offered == maxOffer).Player;
            winner.Points += CurrentCard.Value;
        }

        private void OnNextRoundEvent(EventArgs e)
        {
            NextRoundEvent?.Invoke(this, e);
        }
        public void PlayerReport(Player player, int v)
        {
            if (CurrentCard == null) throw new InvalidOperationException(InitializeAndStartNewGameMessage);

            //check if this card (v) was already used!

            GameAction gameAction = new GameAction(Round, player, v, CurrentCard);
            gameActions.Push(gameAction);
        }
    }
}
