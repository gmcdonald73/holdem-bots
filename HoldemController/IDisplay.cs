using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HoldemPlayerContract;

namespace HoldemController
{
    internal interface IDisplay
    {
        void Initialise(GameConfig gameConfig, int numPlayers, int sleepAfterActionMilliSeconds); 
        void InitHand(int handNum, int numPlayers, List<PlayerInfo> players, int dealerId, int littleBlindSize, int bigBlindSize);
        void DisplayHoleCards(int playerId, Card hole1, Card hole2);
        void DisplayAction(EStage stage, int playerId, EActionType action, int totalAmount, int callAmount, int raiseAmount, bool isAllIn, PotManager potMan);
        void DisplayBoardCard(EBoardCardType cardType, Card boardCard);
        void DisplayPlayerHand(int playerNum, Card hole1, Card hole2, Hand bestHand);
        void DisplayShowdown(HandRanker handRanker, PotManager potMan);
        void DisplayEndOfGame(int numPlayers, List<PlayerInfo> players);
    }
}
