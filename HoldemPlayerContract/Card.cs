namespace HoldemPlayerContract
{
    public class Card
    {
        public Card(ERankType pRank, ESuitType pSuit)
        {
            Rank = pRank;
            Suit = pSuit;
        }
        public readonly ESuitType Suit; 
        public readonly ERankType Rank;

        public string SuitStr()
        {
            switch (Suit)
            {
                case ESuitType.SuitClubs:
                    return "C";
                case ESuitType.SuitHearts:
                    return "H";
                case ESuitType.SuitSpades:
                    return "S";
                case ESuitType.SuitDiamonds:
                    return "D";
                default:
                    return "?";
            }
        }


        public string RankStr()
        {
            switch (Rank)
            {
                case ERankType.RankAce:
                    return "A";
                case ERankType.RankKing:
                    return "K";
                case ERankType.RankQueen:
                    return "Q";
                case ERankType.RankJack:
                    return "J";
                case ERankType.RankTen:
                    return "10";
                case ERankType.RankNine:
                    return "9";
                case ERankType.RankEight:
                    return "8";
                case ERankType.RankSeven:
                    return "7";
                case ERankType.RankSix:
                    return "6";
                case ERankType.RankFive:
                    return "5";
                case ERankType.RankFour:
                    return "4";
                case ERankType.RankThree:
                    return "3";
                case ERankType.RankTwo:
                    return "2";
                default:
                    return "?";
            }
        }

        public string ValueStr()
        {
	        return RankStr() + SuitStr();
        }
    }
}

