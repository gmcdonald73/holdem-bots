using System;
using System.Linq;
using System.Reflection;

using HoldemPlayerContract;

namespace HoldemController
{
    internal class ServerHoldemPlayer : IHoldemPlayer
    {
        private readonly IHoldemPlayer _player;
        public int StackSize { get; set; }
        public bool IsActive { get; set; }
        public bool IsAlive { get; set; }

        public int PlayerNum { get; set; }

        private readonly Card[] _holeCards;

        public ServerHoldemPlayer(int pPlayerNum, int pStackSize, string dllFile)
        {
            PlayerNum = pPlayerNum;
            StackSize = pStackSize;
            IsActive = true;
            IsAlive = true;
            _holeCards = new Card[2];

            var an = AssemblyName.GetAssemblyName(dllFile);
            var assembly = Assembly.Load(an);

            var pluginType = typeof(IHoldemPlayer);
            var types = assembly.GetTypes();
            foreach (var type in types.Where(type => !type.IsInterface
                                                     && !type.IsAbstract
                                                     && type.GetInterface(pluginType.FullName) != null))
            {
                _player = (IHoldemPlayer) Activator.CreateInstance(type);
                InitPlayer(pPlayerNum);
                break;
            }
        }

        public void InitPlayer(int playerNum)
        {
            if (IsObserver)
            {
                IsAlive = false;
                IsActive = false;
                StackSize = 0;
            }

            try
            {
                _player.InitPlayer(playerNum);
            }
            catch (Exception e)
            {
                Logger.Log(string.Format("EXCEPTION: {0} Player {1} : {2}", MethodBase.GetCurrentMethod().Name, playerNum, e.Message));
            }
        }

        public string Name
        {
            get
            {
                var sName = "????";

                try
                {
                    sName = _player.Name;
                }
                catch (Exception e)
                {
                    Logger.Log(string.Format("EXCEPTION: {0} Player {1} : {2}", MethodBase.GetCurrentMethod().Name, PlayerNum, e.Message));
                }

                return sName;
            }
        }

        public bool IsObserver
        {
            get
            {
                var observer = false;

                try
                {
                    observer = _player.IsObserver;
                }
                catch (Exception e)
                {
                    Logger.Log(string.Format("EXCEPTION: {0} Player {1} : {2}", MethodBase.GetCurrentMethod().Name, PlayerNum, e.Message));
                }

                return observer;
            }
        }

        public void InitHand(int numPlayers, PlayerInfo[] players)
        {
            IsActive = IsAlive;

            try
            {
                _player.InitHand(numPlayers, players);
            }
            catch (Exception e)
            {
                Logger.Log(string.Format("EXCEPTION: {0} Player {1} : {2}", MethodBase.GetCurrentMethod().Name, PlayerNum, e.Message));
            }

        }

        public void ReceiveHoleCards(Card hole1, Card hole2)
        {
            _holeCards[0] = hole1;
            _holeCards[1] = hole2;

            try
            {
                _player.ReceiveHoleCards(hole1, hole2);
            }
            catch (Exception e)
            {
                Logger.Log(string.Format("EXCEPTION: {0} Player {1} : {2}", MethodBase.GetCurrentMethod().Name, PlayerNum, e.Message));
            }

        }

        public Card [] HoleCards()
        {
            return _holeCards;
        }

        public void SeeAction(EStage stage, int playerDoingAction, EActionType action, int amount)
        {
            try
            {
                _player.SeeAction(stage, playerDoingAction, action, amount);
            }
            catch (Exception e)
            {
                Logger.Log(string.Format("EXCEPTION: {0} Player {1} : {2}", MethodBase.GetCurrentMethod().Name, PlayerNum, e.Message));
            }

        }

        public void GetAction(EStage stage, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out EActionType playersAction, out int playersBetAmount)
        {
            // Default
            playersAction = EActionType.ActionCall;
            playersBetAmount = callAmount;

            try
            {
                _player.GetAction(stage, callAmount, minRaise, maxRaise, raisesRemaining, potSize, out playersAction, out playersBetAmount);
            }
            catch (Exception e)
            {
                Logger.Log(string.Format("EXCEPTION: {0} Player {1} : {2}", MethodBase.GetCurrentMethod().Name, PlayerNum, e.Message));
            }

            ValidateAction(stage, callAmount, minRaise, maxRaise, raisesRemaining, potSize, ref playersAction, ref playersBetAmount);
        }

        public void ValidateAction(EStage stage, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, ref EActionType playersAction, ref int playersBetAmount)
        {
            // *** Fix up action
            if(stage == EStage.StageShowdown)
            {
                if(playersAction != EActionType.ActionFold)
                {
                    playersAction = EActionType.ActionShow;
                }

                playersBetAmount = 0;
            }
            else
            {
                var bAllIn = false;

                if (playersBetAmount < 0)
                {
                    playersBetAmount = 0;
                }

                if (playersAction == EActionType.ActionRaise)
                {
/*
                    // We shouldn't increase a players bet - but can reduce it if outside limits
                    if (playersBetAmount < minRaise)
                    {
                        playersBetAmount = minRaise;
                    }
*/
                    if (playersBetAmount > maxRaise)
                    {
                        playersBetAmount = maxRaise;
                    }
                }

                if (playersBetAmount > StackSize)
                {
                    playersBetAmount = StackSize;
                }

                if (playersBetAmount == StackSize)
                {
                    bAllIn = true;
                }

                // -- Validate action - prevent player from doing anything illegal
                if (playersAction != EActionType.ActionFold &&
                    playersAction != EActionType.ActionCheck &&
                    playersAction != EActionType.ActionCall &&
                    playersAction != EActionType.ActionRaise )
                {
                    // invalid action - default to call
                    playersAction = EActionType.ActionCall;
                }

                if (playersAction == EActionType.ActionFold && callAmount == 0)
                {
                    // invalid action - don't fold if they can check
                    playersAction = EActionType.ActionCheck;
                }

                if (playersAction == EActionType.ActionCheck && callAmount > 0)
                {
                    // invalid action - can't check so change to call
                    playersAction = EActionType.ActionCall;
                }

                if (playersAction == EActionType.ActionRaise && playersBetAmount <= callAmount)
                {
                    // not enough chips to raise - just call
                    playersAction = EActionType.ActionCall;
                }

                if (playersAction == EActionType.ActionRaise && playersBetAmount > callAmount && playersBetAmount < minRaise && !bAllIn)
                {
                    // not enough chips to raise - just call unless going allin
                    playersAction = EActionType.ActionCall;
                }

                if (playersAction == EActionType.ActionRaise && (raisesRemaining <= 0))
                {
                    // no more raises allowed
                    playersAction = EActionType.ActionCall;
                }

                if (playersAction == EActionType.ActionCall && callAmount == 0)
                {
                    // change call to check if callAmount = 0
                    playersAction = EActionType.ActionCheck;
                }

                // *** Fix betAmount
                if (playersAction == EActionType.ActionFold || playersAction == EActionType.ActionCheck)
                {
                    playersBetAmount = 0;
                }

                if (playersAction == EActionType.ActionCall)
                {
                    playersBetAmount = callAmount;

                    if (playersBetAmount > StackSize)
                    {
                        playersBetAmount = StackSize;
                    }
                }
            }
        }

        public void SeeBoardCard(EBoardCardType cardType, Card boardCard)
        {
            try
            {
                _player.SeeBoardCard(cardType, boardCard);
            }
            catch (Exception e)
            {
                Logger.Log(string.Format("EXCEPTION: {0} Player {1} : {2}", MethodBase.GetCurrentMethod().Name, PlayerNum, e.Message));
            }
        }

        public void SeePlayerHand(int playerShowingHand, Card hole1, Card hole2, Hand bestHand)
        {
            try
            {
                _player.SeePlayerHand(playerShowingHand, hole1, hole2, bestHand);
            }
            catch (Exception e)
            {
                Logger.Log(string.Format("EXCEPTION: {0} Player {1} : {2}", MethodBase.GetCurrentMethod().Name, PlayerNum, e.Message));
            }
        }

        public void EndOfGame(int numPlayers, PlayerInfo[] players)
        {
            try
            {
                _player.EndOfGame(numPlayers, players);
            }
            catch (Exception e)
            {
                Logger.Log(string.Format("EXCEPTION: {0} Player {1} : {2}", MethodBase.GetCurrentMethod().Name, PlayerNum, e.Message));
            }
        }
    }
}
