using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoldemPlayerContract
{

    public enum eSuitType
    {
        SUIT_CLUBS,
        SUIT_HEARTS,
        SUIT_SPADES,
        SUIT_DIAMONDS,
        SUIT_UNKNOWN
    };

    public enum eRankType
    {
        RANK_TWO,
        RANK_THREE,
        RANK_FOUR,
        RANK_FIVE,
        RANK_SIX,
        RANK_SEVEN,
        RANK_EIGHT,
        RANK_NINE,
        RANK_TEN,
        RANK_JACK,
        RANK_QUEEN,
        RANK_KING,
        RANK_ACE,
        RANK_UNKNOWN
    };

    public enum eActionType
    {
        ACTION_BLIND,
        ACTION_FOLD,
        ACTION_CHECK,
        ACTION_CALL,
        ACTION_RAISE,
        ACTION_SHOW,
        ACTION_WIN
    };

    public enum eBoardCardType
    {
        BOARD_FLOP1,
        BOARD_FLOP2,
        BOARD_FLOP3,
        BOARD_TURN,
        BOARD_RIVER,
    }

    public enum eStage
    {
        STAGE_PREFLOP,
        STAGE_FLOP,
        STAGE_TURN,
        STAGE_RIVER,
        STAGE_SHOWDOWN
    }


    public class Card
    {
        public Card(eRankType pRank, eSuitType pSuit)
        {
            Rank = pRank;
            Suit = pSuit;
        }
        public readonly eSuitType Suit; 
        public readonly eRankType Rank; 

        public string SuitStr()
        {
	        switch(Suit)
	        {
	        case eSuitType.SUIT_CLUBS: return "C";
	        case eSuitType.SUIT_HEARTS: return "H";
	        case eSuitType.SUIT_SPADES: return "S";
	        case eSuitType.SUIT_DIAMONDS: return "D";
	        default : return "?";
	        };
        }


        public string RankStr()
        {
	        switch(Rank)
	        {
	        case eRankType.RANK_ACE: return "A";
	        case eRankType.RANK_KING: return "K";
	        case eRankType.RANK_QUEEN: return "Q";
	        case eRankType.RANK_JACK: return "J";
	        case eRankType.RANK_TEN: return "10";
	        case eRankType.RANK_NINE: return "9";
	        case eRankType.RANK_EIGHT: return "8";
	        case eRankType.RANK_SEVEN: return "7";
	        case eRankType.RANK_SIX: return "6";
	        case eRankType.RANK_FIVE: return "5";
	        case eRankType.RANK_FOUR: return "4";
	        case eRankType.RANK_THREE: return "3";
	        case eRankType.RANK_TWO: return "2";
	        default: return "?";
	        };
        }

        public string ValueStr()
        {
	        return RankStr() + SuitStr();
        }

    }

    public class PlayerInfo
    {
        public PlayerInfo(int pPlayerNum, string pName, bool pIsAlive, int pStackSize, bool pIsDealer, bool pIsObserver)
        {
            PlayerNum = pPlayerNum;
            Name = pName;
            IsAlive = pIsAlive;
            StackSize = pStackSize;
            IsDealer = pIsDealer;
            IsObserver = pIsObserver;
        }

        public readonly int PlayerNum;
        public readonly string Name;
        public readonly bool IsAlive;
        public readonly int StackSize;
        public readonly bool IsDealer;
        public readonly bool IsObserver;
    }

    public interface IHoldemPlayer
    {
        void InitPlayer(int playerNum);
        string Name { get; }
        bool IsObserver { get; }
        void InitHand(int numPlayers, PlayerInfo []  players);
        void ReceiveHoleCards(Card hole1, Card hole2);
        void SeeAction(eStage stage, int playerNum, eActionType action, int amount);
        void GetAction(eStage stage, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out eActionType yourAction, out int amount);
        void SeeBoardCard(eBoardCardType cardType, Card boardCard);
        void SeePlayerHand(int playerNum, Card hole1, Card hole2, Hand bestHand);

        void EndOfGame(int numPlayers, PlayerInfo[] players);
    }
}

