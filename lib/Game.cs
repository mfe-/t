using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("t.TestProject1")]
namespace t.lib
{
    public class Game
    {
        private int TotalPointsToPlay = 0;
        private const int CardCapacity = 10;
        private const string InitializeAndStartNewGameMessage = $"Start a new Game with {nameof(NewGame)} and {nameof(Start)}";
        private readonly Random random;
        internal readonly List<GameAction> gameActions = new List<GameAction>();
        public event EventHandler<EventArgs>? GameStartedEvent;
        public event EventHandler<EventArgs>? NextRoundEvent;
        public event EventHandler<EventArgs>? GameEndEvent;
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
        public virtual void MixCards()
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
            TotalPointsToPlay = 0;
            Round = 0;
            CurrentCard = null;
            Players.Clear();
            gameActions.Clear();
            MixCards();
        }
        public void Start(int totalPoints)
        {
            TotalPointsToPlay = totalPoints;
            OnStartEvent(EventArgs.Empty);
            NextRound();
        }
        private void OnStartEvent(EventArgs e)
        {
            GameStartedEvent?.Invoke(this, e);
        }
        public bool NextRound()
        {
            bool nextRound = true;
            if (Round != 0)
            {
                CalculateAndAssignPointsOfPreviousRound();
                nextRound = !IsLastRound();
            }
            OnNextRoundEvent(EventArgs.Empty);
            Round++;
            CurrentCard = Cards[Round - 1];
            return nextRound;
        }

        private bool IsLastRound()
        {
            var winners = Players.Where(a => a.Points >= TotalPointsToPlay);
            if (winners.Any())
            {
                GameEndEvent?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return false;
        }
        private record OfferedDuplicates(int Offered, IEnumerable<GameAction> GameActions);
        internal virtual bool CalculateAndAssignPointsOfPreviousRound()
        {
            if (CurrentCard == null) throw new InvalidOperationException(InitializeAndStartNewGameMessage);
            bool finishedRound = true;
            //retriev all actions of round
            var currentRoundAction = gameActions.Where(a => a.Round == Round);
            var maxOffered = currentRoundAction.Max(a => a.Offered);
            //check for duplicates
            var offeredDuplicates = currentRoundAction.GroupBy(i => i.Offered).Where(g => g.Count() > 1).Select(a => new OfferedDuplicates(a.Key, a));
            if (offeredDuplicates.Any())
            {
                var duplicateMax = offeredDuplicates.Max(a => a.Offered);
                //check if duplicate is higher than single offer
                if (duplicateMax >= maxOffered && offeredDuplicates.Any())
                {
                    finishedRound = false;
                    //loop through playerActions and take the next highest card
                    foreach (var roundAction in currentRoundAction.Where(a => a.Offered != maxOffered).OrderByDescending(a => a.Offered))
                    {
                        if (!offeredDuplicates.Any(a => a.Offered == roundAction.Offered))
                        {
                            maxOffered = roundAction.Offered;
                            finishedRound = true;
                            break;
                        }
                    }
                }
            }

            if (finishedRound)
            {
                var maxOfferbyPlayers = currentRoundAction.Where(a => a.Offered == maxOffered);
                var winner = maxOfferbyPlayers.First();
                //check if previous round was not finished (pair game)
                int previousCardPoints = 0;
                if (gameActions.Where(a => a.Round != Round).Any(a => a.RoundFinished == false))
                {
                    //get previous rounds
                    var previousGameActions = gameActions
                        .Where(a => a.RoundFinished == false)
                            .Where(a => a.Round != Round);

                    previousCardPoints = previousGameActions.Select(a => a.ForCard.Value).Distinct().Sum();

                    foreach (var gameAction in previousGameActions)
                    {
                        gameAction.RoundFinished = finishedRound;
                    }
                }
                //if current card is the same as offered value player recieves double points
                winner.Player.Points += previousCardPoints + (CurrentCard.Value == maxOffered ? CurrentCard.Value * 2 : CurrentCard.Value);
            }

            foreach (var roundAction in currentRoundAction)
            {
                roundAction.RoundFinished = finishedRound;
            }
            return finishedRound;
        }

        private void OnNextRoundEvent(EventArgs e)
        {
            NextRoundEvent?.Invoke(this, e);
        }
        public void PlayerReport(Player player, Card card)
        {
            if (CurrentCard == null) throw new InvalidOperationException(InitializeAndStartNewGameMessage);
            if (gameActions.Where(a => a.Player == player).Any(a => a.Offered == card.Value)) throw new InvalidOperationException("Card used by player twice");
            //check if this card (v) was already used!

            GameAction gameAction = new GameAction(Round, player, card.Value, CurrentCard, false);
            gameActions.Add(gameAction);
        }
        public IEnumerable<Player> EndGame()
        {
            return Players.OrderBy(a => a.Points);
        }
    }
}
