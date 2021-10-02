using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using t.lib.Game;

namespace t.lib.EventArgs
{
    public class MessageReceiveArgs : System.EventArgs
    {
        public MessageReceiveArgs(Func<NextRoundEventArgs, Task> onNextRoundAsyncFunc,
            Func<Task<string>> onChoiceCommandFuncAsync,
            Func<IEnumerable<Card>, Task> showAvailableCardsAsync,
            Func<IEnumerable<Player>, Task> showPlayerWonFunc,
            Func<IEnumerable<Player>, Task> showPlayerStatsFunc,
            Func<Player, int, int, Task> showOfferedByPlayerFunc)
        {
            OnNextRoundAsyncFunc = onNextRoundAsyncFunc;
            OnChoiceCommandFuncAsync = onChoiceCommandFuncAsync;
            ShowAvailableCardsAsync = showAvailableCardsAsync;
            ShowPlayerWonFunc = showPlayerWonFunc;
            ShowPlayerStats = showPlayerStatsFunc;
            ShowOfferedByPlayerFunc = showOfferedByPlayerFunc;
        }
        public Func<NextRoundEventArgs, Task> OnNextRoundAsyncFunc { get; private set; }
        public Func<Task<string>> OnChoiceCommandFuncAsync { get; private set; }
        public Func<IEnumerable<Card>, Task> ShowAvailableCardsAsync { get; private set; }
        public Func<IEnumerable<Player>, Task> ShowPlayerWonFunc { get; }
        public Func<IEnumerable<Player>, Task> ShowPlayerStats { get; }
        public Func<Player, int, int, Task> ShowOfferedByPlayerFunc { get; }
    }
}
