using HoldemPlayerContract;

namespace HoldemController.ConsoleDisplay
{
    internal class UiPlayer
    {
        public UiPlayer(int playerId, string name, bool isAlive, int stackSize)
        {
            PlayerId = playerId;
            Name = name;
            IsAlive = isAlive;
            IsActive = isAlive;
            StackSize = stackSize;
            TotalStageBet = 0;
            HoleCards = new Card[2];
        }

        public int PlayerId { get; }
        public string Name { get; }
        public bool IsDealer { get; set; }
        public bool IsAlive { get; set; }
        public bool IsActive { get; set; }
        public int StackSize { get; set; }
        public int TotalStageBet { get; set; }
        public Card[] HoleCards { get; }
        
    }
}