using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

using HoldemPlayerContract;

namespace HoldemController
{
    class Program
    {
        private ServerHoldemPlayer[] players;
        private Deck deck = new Deck();
        private PotManager _potMan = new PotManager();
        private int _totalMoneyInGame = 0;

        private int dealerPlayerNum = 0;
        private int littleBlindPlayerNum;
        private int bigBlindPlayerNum;

        // Game config - default values
        private int numPlayers = 4;
        private int littleBlindSize = 100;
        private int bigBlindSize = 200;
        private int startingStack = 5000;
        private int maxNumRaisesPerBettingRound = 4; 

        static void Main(string[] args)
        {
            string sExceptionMessage = "";

            try
            {
                Program prog = new Program();
                prog.PlayGame();
            }
            catch (Exception e)
            {
                sExceptionMessage = "EXCEPTION : " + e.Message + "\nPlease send gamelog.txt to gmcdonald73@gmail.com";
                Logger.Log(sExceptionMessage);
            }

            Logger.Close();

            System.Console.WriteLine("-- press any key to exit --");
            ConsoleKeyInfo cki;
            cki = System.Console.ReadKey();

        }

        public void PlayGame()
        {
            LoadConfig();

            bool bDone = false;
            int handNum = 0;

            _totalMoneyInGame = players.Sum(p => p.StackSize);
            dealerPlayerNum = 0;
            littleBlindPlayerNum = GetNextActivePlayer(dealerPlayerNum);
            bigBlindPlayerNum = GetNextActivePlayer(littleBlindPlayerNum);

            while (!bDone)
            {
                Card[] board = new Card[5];
                int lastToAct;

                // init round for each player
                handNum++;
                InitHand(handNum);

                // deal out hole cards to all active players
                DealHoleCards();

                // First betting round - get player actions and broadcast to all players until betting round done
                DoBettingRound(eStage.STAGE_PREFLOP, out lastToAct);

                if (GetNumActivePlayers() > 1)
                {
                    // deal flop
                    board[0] = deck.DealCard();
                    BroadcastBoardCard(eBoardCardType.BOARD_FLOP1, board[0]);

                    board[1] = deck.DealCard();
                    BroadcastBoardCard(eBoardCardType.BOARD_FLOP2, board[1]);

                    board[2] = deck.DealCard();
                    BroadcastBoardCard(eBoardCardType.BOARD_FLOP3, board[2]);

                    // Second betting round - get player actions and broadcast to all players until betting round done
                    if (IsBettingRoundRequired())
                    {
                        DoBettingRound(eStage.STAGE_FLOP, out lastToAct);
                    }
                }

                if (GetNumActivePlayers() > 1)
                {
                    // deal turn
                    board[3] = deck.DealCard();
                    BroadcastBoardCard(eBoardCardType.BOARD_TURN, board[3]);

                    // Third betting round - get player actions and broadcast to all players until betting round done
                    if (IsBettingRoundRequired())
                    {
                        DoBettingRound(eStage.STAGE_TURN, out lastToAct);
                    }
                }

                if (GetNumActivePlayers() > 1)
                {
                    // deal river
                    board[4] = deck.DealCard();
                    BroadcastBoardCard(eBoardCardType.BOARD_RIVER, board[4]);

                    // Fourth betting round - get player actions and broadcast to all players until betting round done
                    if (IsBettingRoundRequired())
                    {
                        DoBettingRound(eStage.STAGE_RIVER, out lastToAct);
                    }
                }

                ViewCash();

                HandRanker handRanker = new HandRanker();

                if (GetNumActivePlayers() > 1)
                {
                    Showdown(board, ref handRanker, lastToAct);
                    handRanker.ViewHandRanks();
                }

                if (GetNumActivePlayers() > 1)
                {
                    // More than one player has shown cards at showdown. Work out how to allocate the pot(s)
                    DistributeWinnings(handRanker);
                }
                else
                {
                    // all players except 1 have folded. Just give entire pot to last man standing
                    int winningPlayer = (players.First(p => (p.IsActive == true))).PlayerNum;

                    players[winningPlayer].StackSize += _potMan.Size();
                    BroadcastAction(eStage.STAGE_SHOWDOWN, winningPlayer, eActionType.ACTION_WIN, _potMan.Size());
                    _potMan.EmptyPot();
                }

                // check that money hasn't disappeared or magically appeared
                ReconcileCash();

                // Kill off broke players & check if only one player left
                KillBrokePlayers();

                if (GetNumLivePlayers() > 1)
                {
                    // Move to next dealer 
                    MoveDealerAndBlinds();
                }
                else
                {
                    bDone = true;
                }

/*
                ConsoleKeyInfo cki;
                cki= System.Console.ReadKey();
                bDone = (cki.Key == ConsoleKey.Escape);
*/
            }

            EndOfGame();

        }

        private void LoadConfig()
        {
            XDocument doc = XDocument.Load("HoldemConfig.xml");

            Logger.Log("--- *** CONFIG *** ---");
            Logger.Log(doc.ToString());

            var gameRules = doc.Element("HoldemConfig").Element("GameRules");

            // Get game rules
            littleBlindSize = Convert.ToInt32(gameRules.Attribute("littleBlind").Value);
            bigBlindSize = Convert.ToInt32(gameRules.Attribute("bigBlind").Value);
            startingStack = Convert.ToInt32(gameRules.Attribute("startingStack").Value);
            maxNumRaisesPerBettingRound = Convert.ToInt32(gameRules.Attribute("maxNumRaisesPerBettingRound").Value); 

            // Create players
            var xplayers = doc.Descendants("Player");
            int i = 0;

            numPlayers = xplayers.Count();
            players = new ServerHoldemPlayer[numPlayers];

            foreach (var player in xplayers)
            {
                int playersStartingStack;
                if (player.Attribute("startingStack") != null)
                {
                    playersStartingStack = Convert.ToInt32(player.Attribute("startingStack").Value);
                }
                else
                {
                    playersStartingStack = startingStack;
                }

                players[i] = new ServerHoldemPlayer(i, playersStartingStack, player.Attribute("dll").Value);
                i++;
            }
        }

        private void InitHand(int handNum)
        {
            PlayerInfo[] playerInfo = new PlayerInfo[numPlayers];

            Logger.Log("");
            Logger.Log("---------*** HAND {0} ***----------", handNum);
            Logger.Log("Num\tName\tIsAlive\tStackSize\tIsDealer");

            for (int i = 0; i < players.Count(); i++)
            {
                Logger.Log("{0}\t{1}\t{2}\t{3}\t{4}", i, players[i].Name, players[i].IsAlive, players[i].StackSize, i == dealerPlayerNum);
                PlayerInfo pInfo = new PlayerInfo(i, players[i].Name.PadRight(20), players[i].IsAlive, players[i].StackSize, i == dealerPlayerNum, players[i].IsObserver);
                playerInfo[i] = pInfo;
            }

            Logger.Log("---------------");

            // broadcast player info to all players
            foreach (ServerHoldemPlayer player in players)
            {
                player.InitHand(players.Count(), playerInfo);
            }

            // shuffle deck
            deck.Shuffle();
        }

        private void EndOfGame()
        {
            PlayerInfo[] playerInfo = new PlayerInfo[numPlayers];

            Logger.Log("");
            Logger.Log("---------*** GAME OVER ***----------");
            Logger.Log("Num\tName\tIsAlive\tStackSize\tIsDealer");

            for (int i = 0; i < players.Count(); i++)
            {
                Logger.Log("{0}\t{1}\t{2}\t{3}\t{4}", i, players[i].Name, players[i].IsAlive, players[i].StackSize, i == dealerPlayerNum);
                PlayerInfo pInfo = new PlayerInfo(i, players[i].Name.PadRight(20), players[i].IsAlive, players[i].StackSize, i == dealerPlayerNum, players[i].IsObserver);
                playerInfo[i] = pInfo;
            }

            Logger.Log("---------------");

            // broadcast player info to all players
            foreach (ServerHoldemPlayer player in players)
            {
                player.EndOfGame(players.Count(), playerInfo);
            }
        }

        private void TakeBlinds()
        {
            // take blinds - inform all players of blinds
            int littleBlindBet = 0;
            int bigBlindBet = 0;

            // little blind might not be live (if big blind was eliminated last hand)
            if (players[littleBlindPlayerNum].IsAlive)
            {
                // check player has enough chips - if not go all in
                if (littleBlindSize > players[littleBlindPlayerNum].StackSize)
                {
                    littleBlindBet = players[littleBlindPlayerNum].StackSize;
                }
                else
                {
                    littleBlindBet = littleBlindSize;
                }

                TransferMoneyToPot(littleBlindPlayerNum, littleBlindBet);
                BroadcastAction(eStage.STAGE_PREFLOP, littleBlindPlayerNum, eActionType.ACTION_BLIND, littleBlindBet);
            }

            // check player has enough chips - if not go all in
            if (bigBlindSize > players[bigBlindPlayerNum].StackSize)
            {
                bigBlindBet = players[bigBlindPlayerNum].StackSize;
            }
            else
            {
                bigBlindBet = bigBlindSize;
            }

            TransferMoneyToPot(bigBlindPlayerNum, bigBlindBet);
            BroadcastAction(eStage.STAGE_PREFLOP, bigBlindPlayerNum, eActionType.ACTION_BLIND, bigBlindBet);
        }

        private void DealHoleCards()
        {
            foreach (ServerHoldemPlayer player in players)
            {
                if (player.IsActive)
                {
                    Card hole1 = deck.DealCard();
                    Card hole2 = deck.DealCard();
                    Logger.Log("Player {0} hole cards {1} {2}", player.PlayerNum, hole1.ValueStr(), hole2.ValueStr());
                    player.ReceiveHoleCards(hole1, hole2);
                }
            }
        }

        private void BroadcastBoardCard(eBoardCardType cardType, Card boardCard)
        {
            Logger.Log("{0} {1}", cardType, boardCard.ValueStr());

            foreach (ServerHoldemPlayer player in players)
            {
                player.SeeBoardCard(cardType, boardCard);
            }
        }

        // broadcast action of a player to all players (including themselves)
        private void BroadcastAction(eStage stage, int playerNumDoingAction, eActionType action, int amount)
        {
            string sLogMsg = string.Format("Player {0} {1} {2}", playerNumDoingAction, action, amount);

            if (players[playerNumDoingAction].StackSize <= 0)
            {
                sLogMsg += " *ALL IN*";
            }

            Logger.Log(sLogMsg);

            foreach (ServerHoldemPlayer player in players)
            {
                player.SeeAction(stage, playerNumDoingAction, action, amount);
            }
        }

        private void BroadcastPlayerHand(int playerNum, Hand playerBestHand)
        {
            Logger.Log("Player {0} Best Hand =  {1} {2}", playerNum, playerBestHand.handValueStr(), playerBestHand.handRankStr());
            Card card1 = players[playerNum].HoleCards()[0];
            Card card2 = players[playerNum].HoleCards()[1];

            foreach (ServerHoldemPlayer player in players)
            {
                player.SeePlayerHand(playerNum, card1, card2, playerBestHand);
            }
        }

        private bool IsBettingRoundRequired()
        {
            // Don't do betting if all players or all but one are all in.
            int numActivePlayersWithChips = players.Count(p => (p.IsActive && (p.StackSize > 0)));

            return (numActivePlayersWithChips > 1);
        }

        private void DoBettingRound(eStage stage, out int lastToAct)
        {
            bool bDone = false;
            int callAmount = 0;
            int minRaise = 0; 
            int maxRaise = 0;
            int raisesRemaining = maxNumRaisesPerBettingRound; 
            eActionType playersAction;
            int playersBetAmount;
            int firstBettorPlayerNum;

            // calc call /raise amounts req
            int lastFullPureRaise = bigBlindSize;
            int callLevel;

            if (stage == eStage.STAGE_PREFLOP)
            {
                TakeBlinds();
                firstBettorPlayerNum = GetNextActivePlayer(bigBlindPlayerNum);
                callLevel = bigBlindSize; //set this explicitly in case the big blind is short
            }
            else
            {
                firstBettorPlayerNum = GetNextActivePlayer(dealerPlayerNum);
                callLevel = _potMan.MaxContributions();
            }

            int currBettor = firstBettorPlayerNum;
            lastToAct = GetPrevActivePlayer(currBettor);

            while (!bDone)
            {
                // dont call GetAction if player is already all in
                if (players[currBettor].StackSize > 0)
                {
                    CalcRequiredBetAmounts(currBettor, callLevel, lastFullPureRaise, out callAmount, out minRaise, out maxRaise);

                    // get the players action
                    players[currBettor].GetAction(stage, callAmount, minRaise, maxRaise, raisesRemaining, _potMan.Size(), out playersAction, out playersBetAmount);

                    // *** DO ACTION ***
                    if (playersAction == eActionType.ACTION_FOLD)
                    {
                        // if fold then mark player as inactive
                        players[currBettor].IsActive = false;
                    }
                    else if ((playersAction == eActionType.ACTION_CALL) || (playersAction == eActionType.ACTION_RAISE))
                    {
                        // if call or raise the take $ from players stack and put in pot
                        TransferMoneyToPot(currBettor, playersBetAmount);

						if (playersAction == eActionType.ACTION_RAISE)
						{
                            // if raise then update lastToAct to the preceding active player
                            lastToAct = GetPrevActivePlayer(currBettor);
                            raisesRemaining--;

                            // if this raise is less than the minimum (because all in) then we shouldn't count it as a proper raise and shouldn't allow the original raiser to reraise
                            if ((playersBetAmount - callAmount) > lastFullPureRaise)
                            {
                                lastFullPureRaise = playersBetAmount - callAmount;
                            }

                            if (_potMan.PlayerContributions(currBettor) > callLevel)
							{
								callLevel = _potMan.PlayerContributions(currBettor);
							}
						}
                    }

                    BroadcastAction(stage, currBettor, playersAction, playersBetAmount);
                }

                // if this player is last to act or only one active player left then bDone = true
                if ((currBettor == lastToAct) || (GetNumActivePlayers() == 1))
                {
                    bDone = true;
                }
                else
                {
                    currBettor = GetNextActivePlayer(currBettor);
                }
            }
        }

        private void CalcRequiredBetAmounts(int currBettor, int callLevel, int lastFullPureRaise, out int callAmount, out int minRaise, out int maxRaise)
        {
            callAmount = callLevel - _potMan.PlayerContributions(currBettor);

            maxRaise = players[currBettor].StackSize; //  if no limit - change this if limit game

            minRaise = callAmount + lastFullPureRaise; 
        }

        private int GetNumActivePlayers()
        {
            return players.Count(p => (p.IsActive == true));
        }

        private int GetNumLivePlayers()
        {
            return players.Count(p => (p.IsAlive == true));
        }

        private int GetNextActivePlayer(int player)
        {
            int i = 0;
            while (i < numPlayers)
            {
                int playerNum = (i + player + 1) % numPlayers;

                if (players[playerNum].IsActive)
                {
                    return playerNum;
                }

                i++;
            }

            // no active players
            return -1;
        }

        private int GetNextLivePlayer(int player)
        {
            int i = 0;
            while (i < numPlayers)
            {
                int playerNum = (i + player + 1) % numPlayers;

                if (players[playerNum].IsAlive)
                {
                    return playerNum;
                }

                i++;
            }

            // no live players
            return -1;
        }


        private int GetPrevActivePlayer(int player)
        {
            int i = 0;
            while (i < numPlayers)
            {
                int playerNum = (numPlayers + player - i - 1) % numPlayers;

                if (players[playerNum].IsActive)
                {
                    return playerNum;
                }

                i++;
            }

            // no active players
            return -1;
        }

        private void TransferMoneyToPot(int playerNum, int amount)
        {
            bool isAllIn;

            if (amount > players[playerNum].StackSize)
            {
                throw new Exception("insufficient chips");
            }

            if (amount > 0)
            {
                players[playerNum].StackSize -= amount;

                isAllIn = (players[playerNum].StackSize == 0);

                _potMan.AddPlayersBet(playerNum, amount, isAllIn);

                ReconcileCash();
            }
        }


        private void ReconcileCash()
        {
            // Check money still adds up
            int totalPlayersStacks = players.Sum(p => p.StackSize);
            int potSize = _potMan.Size();

            if (_totalMoneyInGame != (potSize + totalPlayersStacks))
            {
                ViewCash();
                throw new Exception(string.Format("money doesn't add up! Money in game = {0}, Stacks = {1}, Pots = {2}", _totalMoneyInGame, totalPlayersStacks, potSize));
            }
        }

        private void Showdown(Card[] board, ref HandRanker handRanker, int lastToAct)
        {
            int firstToAct = GetNextActivePlayer(lastToAct);
            Hand playerBestHand;
            List<int> uncontestedPots = new List<int>();

            for (int potNum = 0; potNum < _potMan.Pots.Count(); potNum++)
            {
                uncontestedPots.Add(potNum);
            }

            // evaluate and show hand for first player to act - flag them as winning for now
            playerBestHand = Hand.FindPlayersBestHand(players[firstToAct].HoleCards(), board);
            BroadcastPlayerHand(firstToAct, playerBestHand);

            handRanker.AddHand(firstToAct, playerBestHand);
            uncontestedPots = uncontestedPots.Except( _potMan.GetPotsInvolvedIn(firstToAct)).ToList();

            // Loop through other active players 
            int currPlayer = GetNextActivePlayer(firstToAct);

            do
            {
                ServerHoldemPlayer player = players[currPlayer];
                eActionType playersAction;
                int playersAmount;

                // if not first to act then player may fold without showing cards (unless all in)
                // Also don't allow a player to fold if they are involved in a currently uncontested (side) pot
                List<int> potsInvolvedIn = _potMan.GetPotsInvolvedIn(currPlayer);
                if (player.StackSize > 0 && (uncontestedPots.Intersect(potsInvolvedIn).Count() == 0))
                {
                    player.GetAction(eStage.STAGE_SHOWDOWN, 0, 0, 0, 0, _potMan.Size(), out playersAction, out playersAmount);
                }
                else
                {
                    playersAction = eActionType.ACTION_SHOW;
                }

                if (playersAction == eActionType.ACTION_FOLD)
                {
                    players[currPlayer].IsActive = false;
                    BroadcastAction(eStage.STAGE_SHOWDOWN, currPlayer, playersAction, 0);
                }
                else
                {
                    playerBestHand = Hand.FindPlayersBestHand(player.HoleCards(), board);
                    handRanker.AddHand(currPlayer, playerBestHand);
                    uncontestedPots = uncontestedPots.Except(potsInvolvedIn).ToList();

                    BroadcastPlayerHand(currPlayer, playerBestHand);
                }

                currPlayer = GetNextActivePlayer(currPlayer);

            } while (currPlayer != firstToAct);
        }

        private void DistributeWinnings(HandRanker handRanker)
        {
            foreach(Pot pot in _potMan.Pots)
            {
                // get all players who are involved in this pot
                List<int> playersInvolved = pot.ListPlayersInvolved();

                // loop through hand ranks from highest to lowest
                // find highest ranked handRank that includes at least one of these players
                foreach (HandRankInfo hri in handRanker.HandRanks)
                {
                    List<int> playersInRank = hri.Players;

                    List<int> winningPlayers = playersInvolved.Intersect(playersInRank).ToList();

                    if (winningPlayers.Count() > 0)
                    {
                        // split pot equally between winning players - then handle remainder, remove cash from pot, add to players stack
                        Dictionary<int, int> amountWon = new Dictionary<int, int>();
                        int potRemaining = pot.Size();
                        int shareOfWinnings = potRemaining / winningPlayers.Count();

                        foreach (int i in winningPlayers)
                        {
                            amountWon[i] = shareOfWinnings;
                            potRemaining -= shareOfWinnings;
                        }

                        // if remainder left in pot then allocate 1 chip at a time starting at player in worst position (closest to little blind)
                        int currPlayer = dealerPlayerNum;

                        while (potRemaining > 0)
                        {
                            do
                            {
                                currPlayer = GetNextActivePlayer(currPlayer);
                            } while (!winningPlayers.Contains(currPlayer));

                            amountWon[currPlayer]++;
                            potRemaining--;
                        }

                        pot.EmptyPot();

                        // broadcast win
                        foreach (KeyValuePair<int, int> pair in amountWon)
                        {
                            players[pair.Key].StackSize += pair.Value;
                            BroadcastAction(eStage.STAGE_SHOWDOWN, pair.Key, eActionType.ACTION_WIN, pair.Value);
                        }

                        break;
                    }
                }
            }

            _potMan.EmptyPot();
        }

        private void KillBrokePlayers()
        {
            // Kill off broke players
            foreach (ServerHoldemPlayer player in players)
            {
                if(player.IsAlive && player.StackSize <= 0)
                {
                    Logger.Log("Player {0} has been eliminated", player.PlayerNum);

                    player.IsAlive = false;
                    player.IsActive = false;
                }
            }
        }

        private void MoveDealerAndBlinds()
        {
            dealerPlayerNum = littleBlindPlayerNum; // note that this player might not be live if just eliminated
            littleBlindPlayerNum = bigBlindPlayerNum; // note that this player might not be live if just eliminated
            bigBlindPlayerNum = GetNextLivePlayer(bigBlindPlayerNum);
        }

        public void ViewCash()
        {
            int potNum = 0;

            Logger.Log("");
            Logger.Log("--- View Money ---");

            string sLogMsg;
            // show player numbers
            sLogMsg = "Players\t";

            foreach (ServerHoldemPlayer player in players)
            {
                if (player.IsAlive)
                {
                    sLogMsg += player.PlayerNum + "\t";
                }
            }

            sLogMsg += "Total";
            Logger.Log(sLogMsg);

            // Show stack size
            sLogMsg  = "Stack\t";

            int totalStackSize = 0;
            foreach (ServerHoldemPlayer player in players)
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
            foreach (Pot p in _potMan.Pots)
            {
                int[] playerAmount = new int[players.Count()];

                sLogMsg = "Pot " + potNum + "\t";

                foreach (KeyValuePair<int, int> pair in p.PlayerContributions)
                {
                    playerAmount[pair.Key] = pair.Value;
                }

                foreach (ServerHoldemPlayer player in players)
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
    }
}
