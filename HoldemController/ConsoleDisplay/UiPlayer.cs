using HoldemPlayerContract;

namespace HoldemController.ConsoleDisplay
{
    internal class UiPlayer
    {
        public int PlayerNum {get; set; }
        public string Name  {get; set; }
        public bool IsAlive  {get; set; }
        public bool IsActive  {get; set; }
        public int StackSize  {get; set; }
        public int BetsThisHand  {get; set; }

        public Card[] HoleCards;

        public UiPlayer(int pPlayerNum, string pName, bool pIsAlive, int pStackSize)
        {
            PlayerNum = pPlayerNum;
            Name = pName;
            IsAlive = pIsAlive;
            IsActive = pIsAlive;
            StackSize = pStackSize;
            BetsThisHand = 0;
            HoleCards = new Card[2];
        }
    }
}