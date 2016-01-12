using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoldemPlayerContract
{
    public enum eHandType
    {
        HAND_RUNT,
        HAND_PAIR,
        HAND_TWO_PAIR,
        HAND_THREES,
        HAND_STRAIGHT,
        HAND_FLUSH,
        HAND_FULL_HOUSE,
        HAND_FOURS,
        HAND_STRAIGHT_FLUSH
    };

    public class Hand
    {
        private Card[] _cards;
        private int[] _rankCount;
        private int[] _suitCount;

        private eHandType _rank;
        private eRankType[] _subRank;
        private int _numSubRanks;

        public eHandType handRank()
        {
            return _rank;
        }

        public int numSubRanks()
        {
            return _numSubRanks;
        }

        public eRankType subRank(int level)
        {
            return _subRank[level];
        }


        public Hand(Card[] pCards) 
        {
            if (pCards.Count() != 5)
            {
                return;
            }

            _cards = new Card[5];
            _rankCount = new int[13];
            _suitCount = new int[4];
            _subRank = new eRankType[5];

            for (int i = 0; i < 5; i++)
            {
                _cards[i] = pCards[i];
            }

            _evaluate();

            for (int i = 0; i < 5; i++)
            {
                if (i >= _numSubRanks)
                {
                    _subRank[i] = eRankType.RANK_UNKNOWN;
                }
            }
        }

        public string handValueStr()
        {
            int i;
            string strVal = "";
            string temp;

            for (i = 0; i < 5; i++)
            {
                temp = _cards[i].ValueStr() + " ";
                strVal += temp;
            }

            return strVal;
        }

        public string handRankStr()
        {
            string sRankStr;
            switch (_rank)
            {
                case eHandType.HAND_STRAIGHT_FLUSH: sRankStr = "Straight Flush"; break;
                case eHandType.HAND_FOURS: sRankStr = "Four of a kind"; break;
                case eHandType.HAND_FULL_HOUSE: sRankStr = "Full House"; break;
                case eHandType.HAND_FLUSH: sRankStr = "Flush"; break;
                case eHandType.HAND_STRAIGHT: sRankStr = "Straight"; break;
                case eHandType.HAND_THREES: sRankStr = "Three of a kind"; break;
                case eHandType.HAND_TWO_PAIR: sRankStr = "Two pair"; break;
                case eHandType.HAND_PAIR: sRankStr = "Pair"; break;
                case eHandType.HAND_RUNT: sRankStr = "Runt"; break;
                default: sRankStr = "???"; break;
            };

            /*
                        int i;
                        for (i = 0; i < _numSubRanks; i++)
                        {
                            sRankStr += " " + _subRank[i];
                        }
            */
            return sRankStr;
        }

        // -1 = hand1 is better
        // 0  = hands are equal
        // 1  = hand2 is better
        public static int compareHands(Hand pHand1, Hand pHand2)
        {
            if (pHand1.handRank() > pHand2.handRank())
            {
                return -1;
            }

            if (pHand2.handRank() > pHand1.handRank())
            {
                return 1;
            }

            // Hands are of same rank, compare sub ranks

            //		assert(pHand1.numSubRanks() == pHand2.numSubRanks());

            int numSubRanks = pHand1.numSubRanks();
            int currSubRank;

            for (currSubRank = 0; currSubRank < numSubRanks; currSubRank++)
            {
                if (pHand1.subRank(currSubRank) > pHand2.subRank(currSubRank))
                {
                    return -1;
                }

                if (pHand2.subRank(currSubRank) > pHand1.subRank(currSubRank))
                {
                    return 1;
                }
            }

            return 0;
        }

        // -1 = otherHand is better
        // 0  = hands are equal
        // 1  = this hand is better
        public int compare(Hand otherHand)
        {
            return compareHands(otherHand, this);
        }


        // Given two pocket cards and five board cards,
        // determine the best poker hand that can be formed using any 5 of the 7 cards
        public static Hand FindPlayersBestHand(Card[] pocketCards, Card[] board)
        {
            Card[] cards = new Card[7];
            Card[] currHandCards = new Card[5];
            int i, j, k, currCard;
            Hand bestHand = new Hand(board);  // default to play board
            Hand currHand;

            // Put all cards togther
            cards[0] = pocketCards[0];
            cards[1] = pocketCards[1];
            cards[2] = board[0];
            cards[3] = board[1];
            cards[4] = board[2];
            cards[5] = board[3];
            cards[6] = board[4];

            for (i = 0; i < 7; i++)
            {
                for (j = i + 1; j < 7; j++)
                {
                    // exclude cards at indices i and j, make poker hand
                    // with the other 5 cards
                    currCard = 0;
                    for (k = 0; k < 7; k++)
                    {
                        if ((k != i) && (k != j))
                        {
                            currHandCards[currCard] = cards[k];
                            currCard++;
                        }
                    }

                    currHand = new Hand(currHandCards);

                    // If this is better than current best rank (and sub ranks)
                    // then make this the new best hand
                    if (Hand.compareHands(currHand, bestHand) == -1)
                    {
                        bestHand = currHand;
                    }
                }
            }

            return bestHand;
        }


        // Is this the correct place for this function?
        // This function may need to be customisable to accomodate non-standard hands
        // (e.g. round-the-corner straights, tigers, skeets etc.)
        private void _evaluate()
        {
            int i;

            _sortByRank();
            _calcRankCount();
            _calcSuitCount();

            // Is it a straight flush?
            if (isStraightFlush(ref _numSubRanks, ref _subRank))
            {
                _rank = eHandType.HAND_STRAIGHT_FLUSH;
                return;
            }

            // fours?
            if (isFours(ref _numSubRanks, ref _subRank))
            {
                _rank = eHandType.HAND_FOURS;
                return;
            }

            // full house?
            if (isFullHouse(ref _numSubRanks, ref _subRank))
            {
                _rank = eHandType.HAND_FULL_HOUSE;
                return;
            }

            // flush?
            if (isFlush(ref _numSubRanks, ref _subRank))
            {
                _rank = eHandType.HAND_FLUSH;
                return;
            }

            // straight?
            if (isStraight(ref _numSubRanks, ref _subRank))
            {
                _rank = eHandType.HAND_STRAIGHT;
                return;
            }

            // threes?
            if (isThrees(ref _numSubRanks, ref _subRank))
            {
                _rank = eHandType.HAND_THREES;
                return;
            }

            // two pair?
            if (isTwoPair(ref _numSubRanks, ref _subRank))
            {
                _rank = eHandType.HAND_TWO_PAIR;
                return;
            }

            // one pair?
            if (isPair(ref _numSubRanks, ref _subRank))
            {
                _rank = eHandType.HAND_PAIR;
                return;
            }

            _rank = eHandType.HAND_RUNT;
            _numSubRanks = 5;

            // This works because the hand is already sorted in descending order
            for (i = 0; i < 5; i++)
            {
                _subRank[i] = _cards[i].Rank;
            }
        }


        private bool
        isStraightFlush(ref int numSubRanks, ref eRankType[] subRank)
        {
            // These are required parameters, but can be ignored
            int flushNumSubRanks = 0;
            eRankType[] flushSubRank = new eRankType[5];

            if (isStraight(ref numSubRanks, ref subRank) && isFlush(ref flushNumSubRanks, ref flushSubRank))
            {
                return true;
            }

            return false;
        }


        private bool
        isFours(ref int numSubRanks, ref eRankType[] subRank)
        {
            int currRank;
            int currCard;

            numSubRanks = 2;

            for (currRank = 0; currRank < 13; currRank++)
            {
                if (_rankCount[currRank] == 4)
                {
                    subRank[0] = (eRankType)Enum.ToObject(typeof(eRankType), currRank);

                    for (currCard = 0; currCard < 5; currCard++)
                    {
                        if (_cards[currCard].Rank != subRank[0])
                        {
                            subRank[1] = _cards[currCard].Rank;
                            return true;
                        }
                    }
                }
            }

            return false;
        }


        private bool
        isFullHouse(ref int numSubRanks, ref eRankType[] subRank)
        {
            int threesNumSubRanks = 0;
            eRankType[] threesSubRank = new eRankType[5];

            int pairNumSubRanks = 0;
            eRankType[] pairSubRank = new eRankType[5];


            if (isThrees(ref threesNumSubRanks, ref threesSubRank) &&
                isPair(ref pairNumSubRanks, ref pairSubRank))
            {
                numSubRanks = 2;
                subRank[0] = threesSubRank[0];
                subRank[1] = pairSubRank[0];

                return true;
            }

            return false;
        }


        private bool isFlush(ref int numSubRanks, ref eRankType[] subRank)
        {
            int i;

            numSubRanks = 5;

            for (i = 0; i < 4; i++)
            {
                if ((_suitCount[i] != 0) &&
                    (_suitCount[i] != 5))
                {
                    return false;
                }
            }

            for (i = 0; i < 5; i++)
            {
                subRank[i] = _cards[i].Rank;
            }

            return true;
        }


        private bool isStraight(ref int numSubRanks, ref eRankType[] subRank)
        {
            int i;
            eRankType lowRank = eRankType.RANK_UNKNOWN;
            eRankType highRank = eRankType.RANK_UNKNOWN;

            numSubRanks = 1;

            // if more than one card of the same rank
            // then it is not a straight
            for (i = 0; i < 13; i++)
            {
                if (_rankCount[i] > 1)
                {
                    return false;
                }

                // Determine highest and lowest ranked cards in hand here
                if (_rankCount[i] == 1)
                {
                    highRank = (eRankType)Enum.ToObject(typeof(eRankType), i);

                    if (lowRank == eRankType.RANK_UNKNOWN)
                    {
                        lowRank = (eRankType)Enum.ToObject(typeof(eRankType), i);
                    }
                }
            }

            subRank[0] = highRank;

            // If the highest and lowest rank cards are within a spread of 5 cards, 
            // and there are no pairs, then it must be straight
            if ((highRank - lowRank) == 4)
            {
                return true;
            }

            // Check for Ace low straight here (special case as my code usually treats Ace as high)
            if ((lowRank == eRankType.RANK_TWO) && (highRank == eRankType.RANK_ACE))
            {
                // Got Ace and Two

                // Check for any 6's -> K's. (If any then no straight)
                for (i = (int)eRankType.RANK_SIX; i <= (int)eRankType.RANK_KING; i++)
                {
                    if (_rankCount[i] > 0)
                    {
                        return false;
                    }
                }

                // This is a 5 high straight
                subRank[0] = eRankType.RANK_FIVE;

                return true;
            }

            return false;
        }


        private bool isThrees(ref int numSubRanks, ref eRankType[] subRank)
        {
            eRankType currRank;
            int currCard;
            int currSubRank;

            numSubRanks = 3;

            for (currRank = 0; (int)currRank < 13; currRank++)
            {
                if (_rankCount[(int)currRank] == 3)
                {
                    subRank[0] = currRank;

                    currSubRank = 1;

                    for (currCard = 0; currCard < 5; currCard++)
                    {
                        if (_cards[currCard].Rank != subRank[0])
                        {
                            subRank[currSubRank] = _cards[currCard].Rank;
                            currSubRank++;
                        }
                    }
                    return true;
                }
            }

            return false;
        }


        private bool isTwoPair(ref int numSubRanks, ref eRankType[] subRank)
        {
            eRankType currRank;
            eRankType highPairRank = eRankType.RANK_UNKNOWN;
            eRankType lowPairRank = eRankType.RANK_UNKNOWN;
            eRankType oddCardRank = eRankType.RANK_UNKNOWN;
            int pairCount = 0;

            numSubRanks = 3;

            for (currRank = 0; (int)currRank < 13; currRank++)
            {
                if (_rankCount[(int)currRank] == 2)
                {
                    pairCount++;

                    if (highPairRank != eRankType.RANK_UNKNOWN)
                    {
                        lowPairRank = highPairRank;
                    }

                    highPairRank = currRank;

                }
                else if (_rankCount[(int)currRank] == 1)
                {
                    oddCardRank = currRank;
                }
            }

            if (pairCount == 2)
            {
                subRank[0] = highPairRank;
                subRank[1] = lowPairRank;
                subRank[2] = oddCardRank;

                return true;
            }

            return false;
        }


        private bool isPair(ref int numSubRanks, ref eRankType[] subRank)
        {
            eRankType currRank;
            int currCard;
            int currSubRank;

            numSubRanks = 4;

            for (currRank = 0; (int)currRank < 13; currRank++)
            {
                if (_rankCount[(int)currRank] == 2)
                {
                    subRank[0] = currRank;

                    currSubRank = 1;

                    for (currCard = 0; currCard < 5; currCard++)
                    {
                        if (_cards[currCard].Rank != subRank[0])
                        {
                            subRank[currSubRank] = _cards[currCard].Rank;
                            currSubRank++;
                        }
                    }
                    return true;
                }
            }

            return false;
        }



        // Uses an insertion sort
        private void _sortByRank()
        {
            Card key;
            Card[] sortedCards = new Card[5];
            int i, j;

            sortedCards[0] = _cards[0];

            for (j = 1; j < 5; j++)
            {
                key = _cards[j];

                i = j - 1;

                while ((i >= 0) && (sortedCards[i].Rank < key.Rank))
                {
                    sortedCards[i + 1] = sortedCards[i];
                    i--;
                }

                sortedCards[i + 1] = key;
            }

            for (i = 0; i < 5; i++)
            {
                _cards[i] = sortedCards[i];
            }
        }


        private void _calcRankCount()
        {
            int i;

            // reset all values
            for (i = 0; i < 13; i++)
            {
                _rankCount[i] = 0;
            }

            // count how many of each rank
            for (i = 0; i < 5; i++)
            {
                _rankCount[(int)_cards[i].Rank]++;
            }
        }


        private void _calcSuitCount()
        {
            int i;

            // reset all values
            for (i = 0; i < 4; i++)
            {
                _suitCount[i] = 0;
            }

            // count how many of each suit
            for (i = 0; i < 5; i++)
            {
                _suitCount[(int)_cards[i].Suit]++;
            }
        }
    }
}
