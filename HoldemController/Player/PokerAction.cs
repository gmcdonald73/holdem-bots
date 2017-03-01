using HoldemPlayerContract;

namespace HoldemController.Player
{
    public class PokerAction
    {
        public PokerAction(EActionType action, int amount = 0)
        {
            Action = action;
            Amount = amount;
        }

        public EActionType Action { get; }
        public int Amount { get; }
    }
}