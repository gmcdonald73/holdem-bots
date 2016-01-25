using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HoldemPlayerContract;

namespace HoldemController
{
    class HandRankInfo
    {
        public Hand hand;
        public List<int> Players;

        public HandRankInfo(int playerId, Hand playerHand)
        {
            hand = playerHand;
            Players = new List<int>();
            Players.Add(playerId);
        }

        public void AddPlayer(int playerId)
        {
            if (!Players.Contains(playerId))
            {
                Players.Add(playerId);
            }
        }
    }

    class HandRanker
    {
        public List<HandRankInfo> HandRanks = new List<HandRankInfo>();

        public void AddHand(int playerId, Hand playerHand)
        {
            int i = 0;
            bool bAdded = false;

            while (i < HandRanks.Count() && !bAdded)
            {
                Hand listHand = HandRanks[i].hand;
                int compareResult = playerHand.compare(listHand);

                if ( compareResult == 1)
                {
                    // player hand is better than hand in list so insert before
                    HandRanks.Insert(i, new HandRankInfo(playerId, playerHand));
                    bAdded = true;
                }
                else if (compareResult == 0)
                {
                    // Hands are same value so add playerId to existing handRank
                    HandRanks[i].AddPlayer(playerId);
                    bAdded = true;
                }

                i++;
            }

            if (!bAdded)
            {
                // lowest (or first) hand. Add hand to end of list
                HandRanks.Add(new HandRankInfo(playerId, playerHand));
            }
        }

        public void ViewHandRanks()
        {
            Logger.Log("");
            Logger.Log("--- Hand Ranks ---");

            foreach (HandRankInfo hri in HandRanks)
            {
                Hand hand = hri.hand;
                string sLogMsg;

                sLogMsg = hand.handRankStr() + " ";

                for (int i = 0; i < hand.numSubRanks(); i++)
                {
                    sLogMsg += hand.subRank(i).ToString() + " ";
                }

                sLogMsg += "Players (";

                foreach (int p in hri.Players)
                {
                    sLogMsg += p + ",";
                }

                sLogMsg += ")";
                Logger.Log(sLogMsg);
            }

            Logger.Log("");
        }

    }
}
