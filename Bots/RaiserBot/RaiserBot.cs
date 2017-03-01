using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HoldemPlayerContract;

namespace RaiserBot
{
    // this bot will always bet the minimum raise at every opportunity
    public class RaiserBot : BaseBot
    {
        private int _numRaisesThisStage;
        private int _maxRaisesPerStage = -1;

        public override void InitPlayer(int playerNum, GameConfig gameConfig, Dictionary<string, string> playerConfigSettings)
        {
            if (playerConfigSettings.ContainsKey("maxRaisesPerStage"))
            {
                _maxRaisesPerStage = Convert.ToInt32(playerConfigSettings["maxRaisesPerStage"]);
            }
        }

        public override string Name
        {
            get
            {
                return "RaiserBot";
            }
        }

        public override void InitHand(int handNum, int numPlayers, List<PlayerInfo> players, int dealerId, int littleBlindSize, int bigBlindSize)
        {
            _numRaisesThisStage = 0;
        }

        public override void GetAction(EStage stage, int betSize, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out EActionType yourAction, out int amount)
        {
            if((_maxRaisesPerStage == -1) || (_numRaisesThisStage < _maxRaisesPerStage))
            {
                yourAction = EActionType.ActionRaise;
                amount = minRaise;
                _numRaisesThisStage++;
            }
            else
            {
                yourAction = EActionType.ActionCall;
                amount = callAmount;
            }
        }


        public override void SeeBoardCard(EBoardCardType cardType, Card boardCard)
        {
            _numRaisesThisStage = 0;
        }
    }
}
