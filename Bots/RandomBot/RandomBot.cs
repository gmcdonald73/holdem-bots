using System;
using System.Collections.Generic;
using HoldemPlayerContract;

namespace RandomBot
{
    // this bot will randomly choose between fold, call or minimum raise.
    public class RandomBot : BaseBot
    {
        private int _playerNum;
        private Random _rnd;

        public override void InitPlayer(int playerNum, GameConfig gameConfig, Dictionary<string, string> playerConfigSettings)
        {
            // This is called once at the start of the game. playerNum is your unique identifer for the game
            _playerNum = playerNum;
            int seed = (int)DateTime.Now.Ticks & 0x0000FFFF;
            seed += _playerNum; // to ensure each instance of a random bot gets a different seed
            _rnd = new Random(seed);
        }

        public override string Name
        {
            // return the name of your player
            get
            {
                return "RandomBot";
            }
        }

        public override void GetAction(EStage stage, int betSize, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out EActionType yourAction, out int amount)
        {
            yourAction = EActionType.ActionFold;
            amount = 0;

            // This is the bit where you need to put the AI (mostly likely based on info you receive in other methods)

            if (stage == EStage.StageShowdown)
            {
                // if stage is the showdown then choose whether to show your hand or fold
                yourAction = EActionType.ActionShow;
                amount = 0;
            }
            else
            {
                // stage is preflop, flop, turn or river
                // choose whether to fold, check, call or raise
                // the controller will validate your action and try to honour your action if possible but may change it (e.g. it won't let you fold if checking is possible)
                // amount only matters if you are raising (if calling the controller will use the correct amount). 
                // If raising, minRaise and maxRaise are the total amount required to put into the pot (i.e. it includes the call amount)
                // Side pots are now implemented so you can go all in and call or raise even if you have less than minimum
                int actionNum = _rnd.Next(100);

                if (actionNum < 20)
                {
                    yourAction = EActionType.ActionFold;
                    amount = 0;
                }
                else if (actionNum < 60)
                {
                    yourAction = EActionType.ActionCall;
                    amount = callAmount;
                }
                else
                {
                    yourAction = EActionType.ActionRaise;
                    amount = minRaise;
                }
            }
        }
    }
}
