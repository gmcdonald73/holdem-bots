using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Diagnostics;

using System.IO;
using System.Security;
using System.Security.Policy;
using System.Security.Permissions;
using System.Runtime.Remoting;

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

        private string _sName = null;
        private bool? _bIsObserver = null;

        // for launching GetAction in separate thread
        private EActionType _playersAction;
        private int _playersBetAmount;
        private int _botTimeOutMilliSeconds;
        private Task _task;
        private bool _bIsBotBusy = false; // while a task for the bot is running don't start another task. instead don't send it anymore messages and fold until it is not busy at the start of a hand
        private AppDomain _newDomain;
        private TimeSpan _lastMethodElapsedTime;
        private int _handNum = 0;

        public long LastMethodNumTicks()
        {
            return _lastMethodElapsedTime.Ticks;
        }

        public ServerHoldemPlayer(int sandBoxNum, string dllName)
        {
            string botDir = "bots\\";
            string dllFile = botDir + dllName;

            IsActive = true;
            IsAlive = true;
            _holeCards = new Card[2];

            var an = AssemblyName.GetAssemblyName(dllFile);
            var assembly = Assembly.Load(an);

            var interfaceType = typeof(IHoldemPlayer);
            var types = assembly.GetTypes();
            Type botType = null;

            foreach (var type in types.Where(type => !type.IsInterface
                                                        && !type.IsAbstract
                                                        && type.GetInterface(interfaceType.FullName) != null))
            {
                botType = type;
                break;
            }

            if(botType == null)
            {
                throw new Exception(String.Format("A class that implements the IHoldemPlayer interface was not found in {0}.", dllFile));
            }

            if(dllFile == "bots\\ObserverBot.dll")
            {
                // If bots is trusted then run inside the current app domain
                // !!! need a better way to identify trusted bots !!! use signing?
                _player = (IHoldemPlayer) Activator.CreateInstance(botType);
            }
            else
            {
                // Untrusted bot - create a new sandbox to run it in with limited permissions
                //Setting the AppDomainSetup. It is very important to set the ApplicationBase to a folder 
                //other than the one in which the sandboxer resides.
                AppDomainSetup adSetup = new AppDomainSetup();
                adSetup.ApplicationBase = Path.GetFullPath(botDir);

                //Setting the permissions for the AppDomain. We give the permission to execute and to 
                //read/discover the location where the untrusted code is loaded.
                PermissionSet permSet = new PermissionSet(PermissionState.None);
                permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));

                //Now we have everything we need to create the AppDomain, so let's create it.
                _newDomain = AppDomain.CreateDomain("SandBox" + sandBoxNum, null, adSetup, permSet); 

                // Now create an instance of the bot class inside the new appdomain
                _player = (IHoldemPlayer)_newDomain.CreateInstanceAndUnwrap(an.FullName, botType.FullName);
            }    
        }

        public void InitPlayer(int pPlayerNum, GameConfig gameConfig, Dictionary<string, string> playerConfigSettings)
        {
            PlayerNum = pPlayerNum;
            StackSize =  gameConfig.StartingStack;
            _botTimeOutMilliSeconds = gameConfig.BotTimeOutMilliSeconds;

            if (_botTimeOutMilliSeconds > 0)
            {
                if (!IsBotBusy())
                {
                    _task = Task.Run(() => { RunInitPlayer(pPlayerNum, gameConfig, playerConfigSettings); });

                    // wait X amount of time for task to complete
                    if (!_task.Wait(_botTimeOutMilliSeconds))
                    {
                        // Note that the task is still running in the background
                        _bIsBotBusy = true;
                        Logger.Log("TIMEOUT: {0} Player {1}", MethodBase.GetCurrentMethod().Name, PlayerNum);
                    }
                }
                else
                {
                    // bot is busy still running the previous task
                    Logger.Log("BOT BUSY: {0} Player {1}", MethodBase.GetCurrentMethod().Name, PlayerNum);
                }
            }
            else
            {
                // timeout code disabled - just called method directly
                RunInitPlayer(pPlayerNum, gameConfig, playerConfigSettings);
            }

            TimingLogger.Log(string.Format("{0}, {1}, {2}, {3}, {4:0.0000}", _handNum, EStage.StagePreflop, pPlayerNum, MethodBase.GetCurrentMethod().Name, (double)_lastMethodElapsedTime.Ticks/TimeSpan.TicksPerMillisecond));

            if (IsObserver)
            {
                IsAlive = false;
                IsActive = false;
                StackSize = 0;
            }
        }

        private void RunInitPlayer(int playerNum, GameConfig gameConfig, Dictionary<string, string> playerConfigSettings)
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                _player.InitPlayer(playerNum, gameConfig, playerConfigSettings);
                stopWatch.Stop();
                _lastMethodElapsedTime = stopWatch.Elapsed;
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
                if(_sName == null)
                {
                    _sName = "????"; // default

                    if (_botTimeOutMilliSeconds > 0)
                    {
                        if (!IsBotBusy())
                        {
                            _task = Task.Run(() => { RunGetName(); });

                            // wait X amount of time for task to complete
                            if (!_task.Wait(_botTimeOutMilliSeconds))
                            {
                                // Note that the task is still running in the background
                                _bIsBotBusy = true;
                                Logger.Log("TIMEOUT: {0} Player {1}", MethodBase.GetCurrentMethod().Name, PlayerNum);
                            }
                        }
                        else
                        {
                            // bot is busy still running the previous task
                            Logger.Log("BOT BUSY: {0} Player {1}", MethodBase.GetCurrentMethod().Name, PlayerNum);
                        }
                    }
                    else
                    {
                        // timeout code disabled - just called method directly
                        RunGetName();
                    }

                    TimingLogger.Log(string.Format("{0}, {1}, {2}, {3}, {4:0.0000}, {5}", _handNum, EStage.StagePreflop, PlayerNum, MethodBase.GetCurrentMethod().Name, (double)_lastMethodElapsedTime.Ticks/TimeSpan.TicksPerMillisecond, _sName));

                }

                return _sName;
            }
        }

        private void RunGetName()
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                _sName = _player.Name;
                stopWatch.Stop();
                _lastMethodElapsedTime = stopWatch.Elapsed;
            }
            catch (Exception e)
            {
                _sName = "????";
                Logger.Log(string.Format("EXCEPTION: {0} Player {1} : {2}", MethodBase.GetCurrentMethod().Name, PlayerNum, e.Message));
            }
        }

        public bool IsObserver
        {
            get
            {
                if (_bIsObserver == null)
                {
                    _bIsObserver = true; // default

                    if (_botTimeOutMilliSeconds > 0)
                    {
                        if (!IsBotBusy())
                        {
                            _task = Task.Run(() => { RunIsObserver(); });

                            // wait X amount of time for task to complete
                            if (!_task.Wait(_botTimeOutMilliSeconds))
                            {
                                // Note that the task is still running in the background
                                _bIsBotBusy = true;
                                Logger.Log("TIMEOUT: {0} Player {1}", MethodBase.GetCurrentMethod().Name, PlayerNum);
                            }
                        }
                        else
                        {
                            // bot is busy still running the previous task
                            Logger.Log("BOT BUSY: {0} Player {1}", MethodBase.GetCurrentMethod().Name, PlayerNum);
                        }
                    }
                    else
                    {
                        // timeout code disabled - just called method directly
                        RunIsObserver();
                    }

                    TimingLogger.Log(string.Format("{0}, {1}, {2}, {3}, {4:0.0000}, {5}", _handNum, EStage.StagePreflop, PlayerNum, MethodBase.GetCurrentMethod().Name, (double)_lastMethodElapsedTime.Ticks/TimeSpan.TicksPerMillisecond, _bIsObserver));

                }

                return (bool)_bIsObserver;
            }
        }

        private void RunIsObserver()
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                _bIsObserver = _player.IsObserver;
                stopWatch.Stop();
                _lastMethodElapsedTime = stopWatch.Elapsed;
            }
            catch (Exception e)
            {
                _bIsObserver = true;
                Logger.Log(string.Format("EXCEPTION: {0} Player {1} : {2}", MethodBase.GetCurrentMethod().Name, PlayerNum, e.Message));
            }
        }

        public void InitHand(int handNum, int numPlayers, List<PlayerInfo> players, int dealerId, int littleBlindSize, int bigBlindSize)
        {
            _handNum++;
            IsActive = IsAlive;

            if (_botTimeOutMilliSeconds > 0)
            {
                // if bot is busy at the start of the hand then don't send any messages for the hand (so it doesn't get messages for half a hand)
                _bIsBotBusy = (_task != null && !_task.IsCompleted);

                if (!IsBotBusy())
                {
                    _task = Task.Run(() => { RunInitHand(handNum, numPlayers, players, dealerId, littleBlindSize, bigBlindSize); });

                    // wait X amount of time for task to complete
                    if (!_task.Wait(_botTimeOutMilliSeconds))
                    {
                        // Note that the task is still running in the background
                        _bIsBotBusy = true;
                        Logger.Log("TIMEOUT: {0} Player {1}", MethodBase.GetCurrentMethod().Name, PlayerNum);
                    }
                }
                else
                {
                    // bot is busy still running the previous task
                    Logger.Log("BOT BUSY: {0} Player {1}", MethodBase.GetCurrentMethod().Name, PlayerNum);
                }
            }
            else
            {
                // timeout code disabled - just called method directly
                RunInitHand(handNum, numPlayers, players, dealerId, littleBlindSize, bigBlindSize);
            }

            TimingLogger.Log(string.Format("{0}, {1}, {2}, {3}, {4:0.0000}", _handNum, EStage.StagePreflop, PlayerNum, MethodBase.GetCurrentMethod().Name, (double)_lastMethodElapsedTime.Ticks/TimeSpan.TicksPerMillisecond));

        }

        private void RunInitHand(int handNum, int numPlayers, List<PlayerInfo> players, int dealerId, int littleBlindSize, int bigBlindSize)
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                _player.InitHand(handNum, numPlayers, players, dealerId, littleBlindSize, bigBlindSize);
                stopWatch.Stop();
                _lastMethodElapsedTime = stopWatch.Elapsed;
            }
            catch (Exception e)
            {
                Logger.Log(string.Format("EXCEPTION: {0} Player {1} : {2}", MethodBase.GetCurrentMethod().Name, PlayerNum, e.Message));
            }

        }

        public void ReceiveHoleCards(Card hole1, Card hole2)
        {
            if (_botTimeOutMilliSeconds > 0)
            {
                if (!IsBotBusy())
                {
                    _task = Task.Run(() => { RunReceiveHoleCards(hole1, hole2); });

                    // wait X amount of time for task to complete
                    if (!_task.Wait(_botTimeOutMilliSeconds))
                    {
                        // Note that the task is still running in the background
                        _bIsBotBusy = true;
                        Logger.Log("TIMEOUT: {0} Player {1}", MethodBase.GetCurrentMethod().Name, PlayerNum);
                    }
                }
                else
                {
                    // bot is busy still running the previous task
                    Logger.Log("BOT BUSY: {0} Player {1}", MethodBase.GetCurrentMethod().Name, PlayerNum);
                }
            }
            else
            {
                // timeout code disabled - just called method directly
                RunReceiveHoleCards(hole1, hole2);
            }

            TimingLogger.Log(string.Format("{0}, {1}, {2}, {3}, {4:0.0000}, {5}, {6}", _handNum, EStage.StagePreflop, PlayerNum, MethodBase.GetCurrentMethod().Name, (double)_lastMethodElapsedTime.Ticks/TimeSpan.TicksPerMillisecond, hole1, hole2));

        }

        private void RunReceiveHoleCards(Card hole1, Card hole2)
        {
            _holeCards[0] = hole1;
            _holeCards[1] = hole2;

            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                _player.ReceiveHoleCards(hole1, hole2);
                stopWatch.Stop();
                _lastMethodElapsedTime = stopWatch.Elapsed;
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

        private bool IsBotBusy()
        {
            return _bIsBotBusy;
        }

        public void SeeAction(EStage stage, int playerDoingAction, EActionType action, int amount)
        {
            if (_botTimeOutMilliSeconds > 0)
            {
                if (!IsBotBusy())
                {
                    _task = Task.Run(() => { RunSeeAction(stage, playerDoingAction, action, amount); });

                    // wait X amount of time for task to complete
                    if (!_task.Wait(_botTimeOutMilliSeconds))
                    {
                        // Note that the task is still running in the background
                        _bIsBotBusy = true;
                        Logger.Log("TIMEOUT: {0} Player {1}", MethodBase.GetCurrentMethod().Name, PlayerNum);
                    }
                }
                else
                {
                    // bot is busy still running the previous task
                    Logger.Log("BOT BUSY: {0} Player {1}", MethodBase.GetCurrentMethod().Name, PlayerNum);
                }
            }
            else
            {
                // timeout code disabled - just called method directly
                RunSeeAction(stage, playerDoingAction, action, amount);
            }

            TimingLogger.Log(string.Format("{0}, {1}, {2}, {3}, {4:0.0000}, {5}, {6}, {7}", _handNum, stage, PlayerNum, MethodBase.GetCurrentMethod().Name, (double)_lastMethodElapsedTime.Ticks/TimeSpan.TicksPerMillisecond, playerDoingAction, action, amount));

        }

        private void RunSeeAction(EStage stage, int playerDoingAction, EActionType action, int amount)
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                _player.SeeAction(stage, playerDoingAction, action, amount);
                stopWatch.Stop();
                _lastMethodElapsedTime = stopWatch.Elapsed;
            }
            catch (Exception e)
            {
                Logger.Log(string.Format("EXCEPTION: {0} Player {1} : {2}", MethodBase.GetCurrentMethod().Name, PlayerNum, e.Message));
            }
        }


        public void GetAction(EStage stage, int betSize, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, out EActionType playersAction, out int playersBetAmount)
        {
            // Default to fold if exception or timeout
            playersAction = EActionType.ActionFold;
            playersBetAmount = 0;

            if (_botTimeOutMilliSeconds > 0)
            {
                if (!IsBotBusy())
                {
                    _task = Task.Run(() => { RunGetAction(stage, betSize, callAmount, minRaise, maxRaise, raisesRemaining, potSize);});

                    // wait X amount of time for task to complete
                    // if method has not returned in time then use default action
                    if (_task.Wait(_botTimeOutMilliSeconds))
                    {
                        playersAction = _playersAction;
                        playersBetAmount = _playersBetAmount;
                    }
                    else
                    {
                        // Note that the task is still running in the background
                        Logger.Log("TIMEOUT: {0} Player {1}", MethodBase.GetCurrentMethod().Name, PlayerNum);
                    }
                }
                else
                {
                    // bot is busy still running the previous task
                    Logger.Log("BOT BUSY: {0} Player {1}", MethodBase.GetCurrentMethod().Name, PlayerNum);
                }
            }
            else
            {
                // timeout code disabled - just called method directly
                RunGetAction(stage, betSize, callAmount, minRaise, maxRaise, raisesRemaining, potSize);
                playersAction = _playersAction;
                playersBetAmount = _playersBetAmount;
            }

            ValidateAction(stage, callAmount, minRaise, maxRaise, raisesRemaining, potSize, ref playersAction, ref playersBetAmount);

            TimingLogger.Log(string.Format("{0}, {1}, {2}, {3}, {4:0.0000}, {5}, {6}", _handNum, stage, PlayerNum, MethodBase.GetCurrentMethod().Name, (double)_lastMethodElapsedTime.Ticks/TimeSpan.TicksPerMillisecond, playersAction, playersBetAmount));

        }

        private void RunGetAction(EStage stage, int betSize, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize)
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                // call player.GetAction - can't put out params in anonymous method above so doing it this way
                _player.GetAction(stage, betSize, callAmount, minRaise, maxRaise, raisesRemaining, potSize, out _playersAction, out _playersBetAmount);
                stopWatch.Stop();
                _lastMethodElapsedTime = stopWatch.Elapsed;
            }
            catch (Exception e)
            {
                Logger.Log(string.Format("EXCEPTION: {0} Player {1} : {2}", MethodBase.GetCurrentMethod().Name, PlayerNum, e.Message));
            }
        }


        private void ValidateAction(EStage stage, int callAmount, int minRaise, int maxRaise, int raisesRemaining, int potSize, ref EActionType playersAction, ref int playersBetAmount)
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
            if (_botTimeOutMilliSeconds > 0)
            {
                if (!IsBotBusy())
                {
                    _task = Task.Run(() => { RunSeeBoardCard(cardType, boardCard); });

                    // wait X amount of time for task to complete
                    if (!_task.Wait(_botTimeOutMilliSeconds))
                    {
                        // Note that the task is still running in the background
                        _bIsBotBusy = true;
                        Logger.Log("TIMEOUT: {0} Player {1}", MethodBase.GetCurrentMethod().Name, PlayerNum);
                    }
                }
                else
                {
                    // bot is busy still running the previous task
                    Logger.Log("BOT BUSY: {0} Player {1}", MethodBase.GetCurrentMethod().Name, PlayerNum);
                }
            }
            else
            {
                // timeout code disabled - just called method directly
                RunSeeBoardCard(cardType, boardCard);
            }

            EStage stage = EStage.StageFlop;
            if(cardType == EBoardCardType.BoardRiver)
            {
                stage = EStage.StageRiver;
            }
            else if(cardType == EBoardCardType.BoardTurn)
            {
                stage = EStage.StageTurn;
            }
            
            TimingLogger.Log(string.Format("{0}, {1}, {2}, {3}, {4:0.0000}, {5}, {6}", _handNum, stage, PlayerNum, MethodBase.GetCurrentMethod().Name, (double)_lastMethodElapsedTime.Ticks/TimeSpan.TicksPerMillisecond, cardType, boardCard));

        }
         
        private void RunSeeBoardCard(EBoardCardType cardType, Card boardCard)
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                _player.SeeBoardCard(cardType, boardCard);
                stopWatch.Stop();
                _lastMethodElapsedTime = stopWatch.Elapsed;
            }
            catch (Exception e)
            {
                Logger.Log(string.Format("EXCEPTION: {0} Player {1} : {2}", MethodBase.GetCurrentMethod().Name, PlayerNum, e.Message));
            }
        }

        public void SeePlayerHand(int playerShowingHand, Card hole1, Card hole2, Hand bestHand)
        {
            if (_botTimeOutMilliSeconds > 0)
            {
                if (!IsBotBusy())
                {
                    _task = Task.Run(() => { RunSeePlayerHand(playerShowingHand, hole1, hole2, bestHand); });

                    // wait X amount of time for task to complete
                    if (!_task.Wait(_botTimeOutMilliSeconds))
                    {
                        // Note that the task is still running in the background
                        _bIsBotBusy = true;
                        Logger.Log("TIMEOUT: {0} Player {1}", MethodBase.GetCurrentMethod().Name, PlayerNum);
                    }
                }
                else
                {
                    // bot is busy still running the previous task
                    Logger.Log("BOT BUSY: {0} Player {1}", MethodBase.GetCurrentMethod().Name, PlayerNum);
                }
            }
            else
            {
                // timeout code disabled - just called method directly
                RunSeePlayerHand(playerShowingHand, hole1, hole2, bestHand);
            }

            TimingLogger.Log(string.Format("{0}, {1}, {2}, {3}, {4:0.0000}, {5}, {6}, {7}", _handNum, EStage.StageShowdown, PlayerNum, MethodBase.GetCurrentMethod().Name, (double)_lastMethodElapsedTime.Ticks/TimeSpan.TicksPerMillisecond, playerShowingHand, hole1, hole2));

        }


        private void RunSeePlayerHand(int playerShowingHand, Card hole1, Card hole2, Hand bestHand)
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                _player.SeePlayerHand(playerShowingHand, hole1, hole2, bestHand);
                stopWatch.Stop();
                _lastMethodElapsedTime = stopWatch.Elapsed;
            }
            catch (Exception e)
            {
                Logger.Log(string.Format("EXCEPTION: {0} Player {1} : {2}", MethodBase.GetCurrentMethod().Name, PlayerNum, e.Message));
            }
        }

        public void EndOfGame(int numPlayers, List<PlayerInfo> players)
        {
            if (_botTimeOutMilliSeconds > 0)
            {
                if (!IsBotBusy())
                {
                    _task = Task.Run(() => { RunEndOfGame(numPlayers, players); });

                    // wait X amount of time for task to complete
                    if (!_task.Wait(_botTimeOutMilliSeconds))
                    {
                        // Note that the task is still running in the background
                        _bIsBotBusy = true;
                        Logger.Log("TIMEOUT: {0} Player {1}", MethodBase.GetCurrentMethod().Name, PlayerNum);
                    }
                }
                else
                {
                    // bot is busy still running the previous task
                    Logger.Log("BOT BUSY: {0} Player {1}", MethodBase.GetCurrentMethod().Name, PlayerNum);
                }
            }
            else
            {
                // timeout code disabled - just called method directly
                RunEndOfGame(numPlayers, players);
            }

            TimingLogger.Log(string.Format("{0}, {1}, {2}, {3}, {4:0.0000}", _handNum, EStage.StageShowdown, PlayerNum, MethodBase.GetCurrentMethod().Name, (double)_lastMethodElapsedTime.Ticks/TimeSpan.TicksPerMillisecond));

            // !!! should I be doing this here?
            if(_newDomain != null)
            {
                AppDomain.Unload(_newDomain);
            }
        }

        private void RunEndOfGame(int numPlayers, List<PlayerInfo> players)
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                _player.EndOfGame(numPlayers, players);
                stopWatch.Stop();
                _lastMethodElapsedTime = stopWatch.Elapsed;
            }
            catch (Exception e)
            {
                Logger.Log(string.Format("EXCEPTION: {0} Player {1} : {2}", MethodBase.GetCurrentMethod().Name, PlayerNum, e.Message));
            }
        }
    }
}
