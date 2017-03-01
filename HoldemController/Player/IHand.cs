using HoldemPlayerContract;

namespace HoldemController.Player
{
    public interface IHand
    {
        EStage Stage { get; }
        Card[] CommunityCards { get; }

        int SmallBlind { get; }
        int BigBlind { get; }
    }
}