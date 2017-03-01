using HoldemPlayerContract;

namespace HoldemController.Player
{
    public class PokerActionHistory : PokerAction
    {
        public PokerActionHistory(EStage stage, EActionType action, int amount) : base(action, amount)
        {
            Stage = stage;
        }
        
        public EStage Stage { get; }
    }
}