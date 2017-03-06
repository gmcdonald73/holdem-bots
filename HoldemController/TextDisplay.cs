using System.Collections.Generic;
using HoldemPlayerContract;

namespace HoldemController
{
    internal class TextDisplay : IDisplay
    {
        public void SetWriteToConsole(bool writeToConsole)
        {
            Logger.SetWriteToConsole(writeToConsole);
        }

        public void Initialise(GameConfig gameConfig, int numPlayers, int sleepAfterActionMilliSeconds)
        {

        }

        public void InitHand(int handNum, int numPlayers, List<PlayerInfo> players, int dealerId, int smallBlindSize, int bigBlindSize)
        {
            Logger.Log("");
            Logger.Log("---------*** HAND {0} ***----------", handNum);
            Logger.Log("Num\tName\tIsAlive\tStackSize\tIsDealer");

            foreach(PlayerInfo p in players)
            {
                Logger.Log("{0}\t{1}\t{2}\t{3}\t{4}", p.PlayerNum, p.Name, p.IsAlive, p.StackSize, p.PlayerNum == dealerId);
            }
        }

        public void DisplayHoleCards(int playerId, Card hole1, Card hole2)
        {
            Logger.Log("Player {0} hole cards {1} {2}", playerId, hole1.ValueStr(), hole2.ValueStr());
        }

        public void DisplayAction(Stage stage, int playerId, ActionType action, int totalAmount, int betSize, int callAmount, int raiseAmount, bool isAllIn, PotManager potMan)
        {
            string sLogMsg = "";

            sLogMsg += string.Format("Player {0} {1} {2}", playerId, action, totalAmount);

            if (action == ActionType.Raise)
            {
                sLogMsg += string.Format(" ({0} call + {1} raise)", callAmount, raiseAmount);
            }

            if (isAllIn)
            {
                sLogMsg += " *ALL IN*";
            }

            Logger.Log(sLogMsg);
        }

        public void DisplayBoardCard(EBoardCardType cardType, Card boardCard)
        {
            Logger.Log("{0} {1}", cardType, boardCard.ValueStr());
        }

        public void DisplayPlayerHand(int playerNum, Card hole1, Card hole2, Hand bestHand)
        {
            Logger.Log("Player {0} Best Hand =  {1} {2}", playerNum, bestHand.HandValueStr(), bestHand.HandRankStr());
        }

        public void DisplayShowdown(HandRanker handRanker, PotManager potMan)
        {
            Logger.Log("");
            Logger.Log("--- Hand Ranks ---");

            foreach (var hri in handRanker.HandRanks)
            {
                var hand = hri.Hand;

                var sLogMsg = hand.HandRankStr() + " ";

                for (var i = 0; i < hand.NumSubRanks(); i++)
                {
                    sLogMsg += hand.SubRank(i) + " ";
                }

                sLogMsg += "Players (" + string.Join(",", hri.Players) + ")";
                Logger.Log(sLogMsg);
            }

            Logger.Log("");
        }

        public void DisplayEndOfGame(int numPlayers, List<PlayerInfo> players)
        {
            Logger.Log("");
            Logger.Log("---------*** GAME OVER ***----------");
            Logger.Log("Num\tName\tIsAlive\tStackSize");

            foreach(PlayerInfo p in players)
            {
                Logger.Log("{0}\t{1}\t{2}\t{3}", p.PlayerNum, p.Name, p.IsAlive, p.StackSize);
            }

            Logger.Log("---------------");
        }

/*
        public void ViewCash()
        {
            var potNum = 0;

            Logger.Log("");
            Logger.Log("--- View Money ---");

            string sLogMsg;
            // show player numbers
            sLogMsg = "Players\t";

            foreach (var player in _players)
            {
                if (player.IsAlive)
                {
                    sLogMsg += player.PlayerNum + "\t";
                }
            }

            sLogMsg += "Total";
            Logger.Log(sLogMsg);

            // Show stack size
            sLogMsg = "Stack\t";

            var totalStackSize = 0;
            foreach (var player in _players)
            {
                if (player.IsAlive)
                {
                    sLogMsg += player.StackSize + "\t";
                    totalStackSize += player.StackSize;
                }
            }

            sLogMsg += totalStackSize;
            Logger.Log(sLogMsg);

            // Show breakdown of each pot
            foreach (var p in _potMan.Pots)
            {
                var playerAmount = new int[_players.Length];

                sLogMsg = "Pot " + potNum + "\t";

                foreach (var pair in p.PlayerContributions)
                {
                    playerAmount[pair.Key] = pair.Value;
                }

                foreach (var player in _players)
                {
                    if (player.IsAlive)
                    {
                        sLogMsg += playerAmount[player.PlayerNum] + "\t";
                    }
                }

                sLogMsg += p.Size();
                Logger.Log(sLogMsg);

                potNum++;
            }

            Logger.Log("");
        }
*/
    }
}
