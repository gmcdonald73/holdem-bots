namespace HoldemPlayerContract.Player
{
    public interface IHand
    {
        Stage Stage { get; }
        Card[] CommunityCards { get; }

        int SmallBlind { get; }
        int BigBlind { get; }
    }
}