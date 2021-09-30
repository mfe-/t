using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("t.TestProject1")]
namespace t.lib
{
    public class GameLogic
    {
        private int TotalPointsToPlay = 0;
        public const int CardCapacity = 10;
        private const string InitializeAndStartNewGameMessage = $"Start a new Game with {nameof(NewGame)} and {nameof(Start)}";
        private readonly Random random;
        internal readonly List<GameAction> gameActions = new List<GameAction>();
        public event EventHandler<EventArgs>? GameStartedEvent;
        public event EventHandler<NextRoundEventArgs>? NextRoundEvent;
        public event EventHandler<EventArgs>? GameEndEvent;
        public event EventHandler<EventArgs<Player>>? NewPlayerRegisteredEvent;
        public event EventHandler<EventArgs>? RequiredAmountOfPlayersReachedEvent;
        public GameLogic()
        {
            random = new Random();
            Cards = new List<Card>(CardCapacity);
            PlayerCards = new Dictionary<Player, IList<Card>>();
            Players = new List<Player>();
        }
        /// <summary>
        /// The amount of players.
        /// As the long the amount of required players is not fulfilled, the game cant be started
        /// </summary>
        public int RequiredAmountOfPlayers { get; private set; } = 0;
        /// <summary>
        /// a list of all players
        /// </summary>
        public IList<Player> Players { get; private set; }
        /// <summary>
        /// the list of cards which are played for
        /// </summary>
        public IList<Card> Cards { get; private set; }
        /// <summary>
        /// contains the list of cards which the player can use. 
        /// </summary>
        public IDictionary<Player, IList<Card>> PlayerCards { get; private set; }
        /// <summary>
        /// the current round
        /// </summary>
        public int Round { get; private set; }
        /// <summary>
        /// the card to play for (from <see cref="Cards"/>
        /// </summary>
        public Card? CurrentCard { get; private set; }
        /// <summary>
        /// register a player for the upcoming game
        /// </summary>
        /// <param name="player"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public virtual void RegisterPlayer(Player player)
        {
            if (player == null) throw new ArgumentNullException();
            if (Cards.Count == 0) throw new InvalidOperationException(InitializeAndStartNewGameMessage);
            if (Players.Count == CardCapacity) throw new InvalidOperationException($"Game only designed for {CardCapacity} players");
            Players.Add(player);
            AddPlayerCards(player);
            NewPlayerRegisteredEvent?.Invoke(this, new EventArgs<Player>(player));
            if (Players.Count == RequiredAmountOfPlayers)
            {
                RequiredAmountOfPlayersReachedEvent?.Invoke(this, EventArgs.Empty);
            }
        }
        /// <summary>
        /// adds the card for a player
        /// </summary>
        /// <param name="player"></param>
        private void AddPlayerCards(Player player)
        {
            var playerCards = new List<Card>();
            for (int i = 1; i < CardCapacity + 1; i++)
            {
                Card card = new Card(i);
                playerCards.Add(card);
            }
            PlayerCards.Add(player, playerCards);
        }

        public void OnLeavePlayerEvent(Player player)
        {
            Players.Remove(player);
        }
        /// <summary>
        /// Mixes the cards from <see cref="Cards"/>
        /// </summary>
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
        /// <summary>
        /// prepares a new game. In this phase player can use <see cref="RegisterPlayer(Player)"/> 
        /// </summary>
        /// <param name="requiredAmountOfPlayers"></param>
        /// <exception cref="ArgumentException"></exception>
        public void NewGame(int requiredAmountOfPlayers)
        {
            if (requiredAmountOfPlayers < 1) throw new ArgumentException("At least two players are required to play the game!");
            RequiredAmountOfPlayers = requiredAmountOfPlayers;
            TotalPointsToPlay = 0;
            Round = 0;
            CurrentCard = null;
            Players.Clear();
            PlayerCards.Clear();
            gameActions.Clear();
            MixCards();
        }
        /// <summary>
        /// Starts the game
        /// </summary>
        /// <param name="totalPoints"></param>
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
            if (nextRound)
            {
                Round++;
                CurrentCard = Cards[Round - 1];
                OnNextRoundEvent(new NextRoundEventArgs(Round, CurrentCard));
            }
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

        private void OnNextRoundEvent(NextRoundEventArgs e)
        {
            NextRoundEvent?.Invoke(this, e);
        }
        public void PlayerReport(Player player, Card card)
        {
            if (CurrentCard == null) throw new InvalidOperationException(InitializeAndStartNewGameMessage);
            //check if this card (v) was already used!
            if (gameActions.Where(a => a.Player == player).Any(a => a.Offered == card.Value)) throw new InvalidOperationException("Card used by player twice");
            PlayerCards[player].Remove(card);
            GameAction gameAction = new GameAction(Round, player, card.Value, CurrentCard, false);
            gameActions.Add(gameAction);
        }
        /// <summary>
        /// returns the list of players which should pick a card for the current round
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Player> GetRemainingPickCardPlayers()
        {
            var gameActionsCurrent = gameActions.Where(a => a.Round == Round);
            foreach (var player in Players)
            {
                if (!gameActionsCurrent.Any(a => a.Player == player))
                    yield return player;
            }
        }
        public IEnumerable<Player> GetPlayerStats()
        {
            return Players.OrderBy(a => a.Points);
        }
    }
}
