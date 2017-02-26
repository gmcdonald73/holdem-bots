using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HoldemPlayerContract;

namespace AllInBot
{
    // this bot always goes allin at every opportunity.
    // This is especially useful when testing side pots are working correctly. i.e. use multiple allin bots with different starting stack sizes
    public class AllInBot : BaseBot
    {
        public override string Name
        {
            get
            {
                return "AllInBot";
            }
        }

        public override void GetAction(EStage stage, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out EActionType yourAction, out int amount)
        {
            yourAction = EActionType.ActionRaise;
            amount = maxRaise;
        }
    }
}
