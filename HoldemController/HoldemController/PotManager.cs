using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoldemController
{
    class Pot
    {
        private int _potSize;  // should match sum off all player contributions - unless we have distributed winnings
        private int _maxContributions = 0;
        private bool _IsCapped = false;

        // need to record contributions per player
        public Dictionary<int, int> PlayerContributions = new Dictionary<int, int>();

        public int NumPlayersInvolved = 0;

        public void EmptyPot()
        {
            _potSize = 0;
            _maxContributions = 0;
            PlayerContributions.Clear();
            NumPlayersInvolved = 0;
            _IsCapped = false;
        }

        public void CapPot()
        {
            _IsCapped = true;
        }

        public bool IsCapped()
        {
            return _IsCapped;
        }

        public void AddPlayersBet(int playerId, int amount)
        {
            int contributions = 0;

            if (PlayerContributions.ContainsKey(playerId))
            {
                contributions = PlayerContributions[playerId];
            }
            else
            {
                NumPlayersInvolved++;
            }

            // Not an error - This could validly occur if splitting a pot and another player has not yet called the pot that is being split
            //if (amount < (_maxContributions - contributions))
            //{
            //    throw new Exception("insufficient amount to call");
            //}

            contributions += amount;

            if (contributions > _maxContributions)
            {
                if (_IsCapped)
                {
                    throw new Exception("contribution is too big, pot is capped");
                }
                else
                {
                    _maxContributions = contributions;
                }
            }

            PlayerContributions[playerId] = contributions;
            _potSize += amount;
        }

        public int Size()
        {
            return _potSize;
        }
        public int MaxContributions()
        {
            return _maxContributions;
        }

        public int AmountToCall(int playerId)
        {
            // max total contributions by any player - contributions for this player
            int contributions = 0;

            PlayerContributions.TryGetValue(playerId, out contributions);

            return _maxContributions - contributions;
        }

        public List<int> ListPlayersInvolved()
        {
            List<int> playersInvolved = new List<int>();

            // get list of all players that have contributed to the pot
            foreach (KeyValuePair<int, int> pair in PlayerContributions)
            {
                playersInvolved.Add(pair.Key);
            }

            return playersInvolved;
        }
    }

    class PotManager
    {
        public List<Pot> Pots = new List<Pot>();

        public void AddPlayersBet(int playerId, int betAmount, bool isAllIn)
        {
            if (betAmount == 0) return;

            if (Pots.Count() == 0)
            {
                Pots.Add(new Pot());
            }
            // work out which side pots to call. create new side pots if required

            // loop through pots from most players involved to least
            // call each pot if able (and raise the last if able). if allin and unable to call a pot then split this pot in two
            int betAmountRemaining = betAmount;

            int potNum = 0;
            int numPots = Pots.Count();

            while (potNum < numPots && betAmountRemaining > 0)
            {
                Pot p = Pots[potNum];

                int amountToCall = p.AmountToCall(playerId);

                if (betAmountRemaining >= amountToCall)
                {
                    //enough chips to call or raise pot
                    if (p.IsCapped())
                    {
                        // call this pot and try to put remainder in next one
                        p.AddPlayersBet(playerId, amountToCall);
                        betAmountRemaining -= amountToCall;
                    }
                    else
                    {
                        // pot is not capped so bet full raise into this one
                        p.AddPlayersBet(playerId, betAmountRemaining);
                        betAmountRemaining = 0;

                        if (isAllIn)
                        {
                            // if this player is allin then cap this pot so no other player may increase it
                            p.CapPot();
                        }
                    }
                }
                else
                {
                    // not enough to call - player better be going all in
                    if(!isAllIn)
                    {
                        throw new Exception("insufficient chips to call");
                    }

                    // split current pot and cap it, add players bet to new pot
                    SplitPotAndAddBet(p, playerId, betAmountRemaining);
                    betAmountRemaining = 0;

                }

                potNum++;
            }

            // all pots capped so need to create a new one to put remaining chips in
            if (betAmountRemaining > 0)
            {
                Pot newPot = new Pot();
                newPot.AddPlayersBet(playerId, betAmountRemaining);
                betAmountRemaining = 0;

                if (isAllIn)
                {
                    // if this player is allin then cap this pot so no other player may increase it
                    newPot.CapPot();
                }

                Pots.Add(newPot);
            }

        }

        private void SplitPotAndAddBet(Pot p, int playerId, int betAmount)
        {
            Pot newPot1 = new Pot();
            Pot newPot2 = new Pot();

            int pastContribution = 0;
            if (p.PlayerContributions.ContainsKey(playerId))
            {
                pastContribution = p.PlayerContributions[playerId];
            }

            // this is the players total contribution to this pot including the current bet. This is the amount we need to split on
            int totalContribution = betAmount + pastContribution;
            int capAmount = totalContribution;

            foreach (KeyValuePair<int, int> pair in p.PlayerContributions)
            {
                if (pair.Key != playerId)
                {
                    if (pair.Value > capAmount)
                    {
                        newPot1.AddPlayersBet(pair.Key, capAmount);
                        newPot2.AddPlayersBet(pair.Key, pair.Value - capAmount);
                    }
                    else
                    {
                        newPot1.AddPlayersBet(pair.Key, pair.Value);
                    }
                }
            }

            newPot1.AddPlayersBet(playerId, totalContribution);
            betAmount = 0;

            newPot1.CapPot();

            if (p.IsCapped())
            {
                newPot2.CapPot();
            }

            Pots.Remove(p);
            Pots.Add(newPot1);
            Pots.Add(newPot2);

            Pots.Sort(delegate(Pot x, Pot y)
            {
                if (x.NumPlayersInvolved > y.NumPlayersInvolved) return -1;
                else if (x.NumPlayersInvolved < y.NumPlayersInvolved) return 1;
                else return 0;
            });

        }

        public void EmptyPot()
        {
            Pots.Clear();
        }


        public int Size()
        {
            int size = 0;

            foreach (Pot p in Pots)
            {
                size += p.Size();
            }

            return size;
        }

        public int AmountToCall(int playerId)
        {
            int amountToCall = 0;

            foreach (Pot p in Pots)
            {
                amountToCall += p.AmountToCall(playerId);
            }

            return amountToCall;
        }

        public List<int> GetPotsInvolvedIn(int playerNum)
        {
            List <int> potsInvolvedIn = new List<int>();
            int potNum = 0;

            for(potNum=0; potNum<Pots.Count(); potNum++)
            {
                if (Pots[potNum].ListPlayersInvolved().Contains(playerNum))
                {
                    potsInvolvedIn.Add(potNum);
                }
            }

            return potsInvolvedIn;
        }

        public int MaxContributions()
        {
            int maxContributions = 0;

            foreach (Pot p in Pots)
            {
                maxContributions += p.MaxContributions();
            }

            return maxContributions;
        }

        public int PlayerContributions(int playerNum)
        {
            int playerContributions = 0;

            foreach (Pot p in Pots)
            {
                if (p.PlayerContributions.ContainsKey(playerNum))
                {
                    playerContributions += p.PlayerContributions[playerNum];
                }
            }

            return playerContributions;
        }

    }
}
