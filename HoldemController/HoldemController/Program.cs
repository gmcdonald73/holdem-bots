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

        private int dealerPlayerNum = 0;
        private int littleBlindPlayerNum;
        private int bigBlindPlayerNum;

        private int maxBetsThisBettingRound = 0;
        private int potSize = 0;

        // Game config - default values
        private int numPlayers = 4;
        private int littleBlindSize = 100;
        private int bigBlindSize = 200;
        private int startingStack = 5000;
        private int maxNumRaisesPerBettingRound = 4; 

        static void Main(string[] args)
        {
            Program prog = new Program();

            prog.PlayGame();
        }

        public void PlayGame()
        {
            LoadConfig();

            bool bDone = false;
            int handNum = 0;

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

                // deal out hold cards to all active players
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
                    DoBettingRound(eStage.STAGE_FLOP, out lastToAct);
                }

                if (GetNumActivePlayers() > 1)
                {
                    // deal turn
                    board[3] = deck.DealCard();
                    BroadcastBoardCard(eBoardCardType.BOARD_TURN, board[3]);

                    // Third betting round - get player actions and broadcast to all players until betting round done
                    DoBettingRound(eStage.STAGE_TURN, out lastToAct);
                }

                if (GetNumActivePlayers() > 1)
                {
                    // deal river
                    board[4] = deck.DealCard();
                    BroadcastBoardCard(eBoardCardType.BOARD_RIVER, board[4]);

                    // Fourth betting round - get player actions and broadcast to all players until betting round done
                    DoBettingRound(eStage.STAGE_RIVER, out lastToAct);
                }

                List<int> winningPlayers = new List<int>();

                if (GetNumActivePlayers() > 1)
                {
                    Showdown(board, ref winningPlayers, lastToAct);
                }
                else
                {
                    winningPlayers.Add((players.First(p => (p.IsActive == true))).PlayerNum);
                }

                DistributeWinnings(winningPlayers);

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

            ConsoleKeyInfo cki;
            cki = System.Console.ReadKey();

        }

        private void LoadConfig()
        {
            XDocument doc = XDocument.Load("HoldemConfig.xml");

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
                Console.WriteLine(player.Value);
                players[i] = new ServerHoldemPlayer(i, startingStack, player.Attribute("dll").Value);
                i++;
            }
        }

        private void InitHand(int handNum)
        {
            PlayerInfo[] playerInfo = new PlayerInfo[numPlayers];

            System.Console.WriteLine("---------*** HAND {0} ***----------", handNum);
            System.Console.WriteLine("Num\tName\tIsAlive\tStackSize\tIsDealer");

            for (int i = 0; i < players.Count(); i++)
            {
                System.Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", i, players[i].Name, players[i].IsAlive, players[i].StackSize, i == dealerPlayerNum);
                PlayerInfo pInfo = new PlayerInfo(i, players[i].Name.PadRight(20), players[i].IsAlive, players[i].StackSize, i == dealerPlayerNum, players[i].IsObserver);
                playerInfo[i] = pInfo;
            }

            System.Console.WriteLine("---------------");

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

            System.Console.WriteLine("---------*** GAME OVER ***----------");
            System.Console.WriteLine("Num\tName\tIsAlive\tStackSize\tIsDealer");

            for (int i = 0; i < players.Count(); i++)
            {
                System.Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", i, players[i].Name, players[i].IsAlive, players[i].StackSize, i == dealerPlayerNum);
                PlayerInfo pInfo = new PlayerInfo(i, players[i].Name.PadRight(20), players[i].IsAlive, players[i].StackSize, i == dealerPlayerNum, players[i].IsObserver);
                playerInfo[i] = pInfo;
            }

            System.Console.WriteLine("---------------");

            // broadcast player info to all players
            foreach (ServerHoldemPlayer player in players)
            {
                player.EndOfGame(players.Count(), playerInfo);
            }
        }

        private void TakeBlinds()
        {
            // take blinds - inform all players of blinds

            // little blind might not be live (if big blind was eliminated last hand)
            if (players[littleBlindPlayerNum].IsAlive)
            {
                TransferMoneyToPot(littleBlindPlayerNum, littleBlindSize);
                BroadcastAction(eStage.STAGE_PREFLOP, littleBlindPlayerNum, eActionType.ACTION_BLIND, littleBlindSize);
            }

            TransferMoneyToPot(bigBlindPlayerNum, bigBlindSize);
            BroadcastAction(eStage.STAGE_PREFLOP, bigBlindPlayerNum, eActionType.ACTION_BLIND, bigBlindSize);
        }

        private void DealHoleCards()
        {
            foreach (ServerHoldemPlayer player in players)
            {
                if (player.IsActive)
                {
                    Card hole1 = deck.DealCard();
                    Card hole2 = deck.DealCard();
                    System.Console.WriteLine("Player {0} hole cards {1} {2}", player.PlayerNum, hole1.ValueStr(), hole2.ValueStr());
                    player.ReceiveHoleCards(hole1, hole2);
                }
            }
        }

        private void BroadcastBoardCard(eBoardCardType cardType, Card boardCard)
        {
            System.Console.WriteLine("{0} {1}", cardType, boardCard.ValueStr());

            foreach (ServerHoldemPlayer player in players)
            {
                player.SeeBoardCard(cardType, boardCard);
            }
        }

        // broadcast action of a player to all players (including themselves)
        private void BroadcastAction(eStage stage, int playerNumDoingAction, eActionType action, int amount)
        {
            System.Console.WriteLine("Player {0} {1} {2}", playerNumDoingAction, action, amount);

            foreach (ServerHoldemPlayer player in players)
            {
                player.SeeAction(stage, playerNumDoingAction, action, amount);
            }
        }

        private void BroadcastPlayerHand(int playerNum, Hand playerBestHand)
        {
            System.Console.WriteLine("Player {0} Best Hand =  {1} {2}\n", playerNum, playerBestHand.handValueStr(), playerBestHand.handRankStr());
            Card card1 = players[playerNum].HoleCards()[0];
            Card card2 = players[playerNum].HoleCards()[1];

            foreach (ServerHoldemPlayer player in players)
            {
                player.SeePlayerHand(playerNum, card1, card2, playerBestHand);
            }
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

            maxBetsThisBettingRound = 0;

            foreach (ServerHoldemPlayer player in players)
            {
                player.BetsThisBettingRound = 0;
            }

            if (stage == eStage.STAGE_PREFLOP)
            {
                TakeBlinds();
                firstBettorPlayerNum = GetNextActivePlayer(bigBlindPlayerNum);
            }
            else
            {
                firstBettorPlayerNum = GetNextActivePlayer(dealerPlayerNum);
            }

            int currBettor = firstBettorPlayerNum;
            lastToAct = GetPrevActivePlayer(currBettor);

            while (!bDone)
            {
                callAmount = maxBetsThisBettingRound - players[currBettor].BetsThisBettingRound;
                minRaise = callAmount + bigBlindSize; // !!! this may not be correct - may need to use last raise size depending on rules
                maxRaise = players[currBettor].StackSize; //  if no limit - change this if limit game

                players[currBettor].GetAction(stage, callAmount, minRaise, maxRaise, raisesRemaining, potSize, out playersAction, out playersBetAmount);

                // *** DO ACTION ***
                if(playersAction == eActionType.ACTION_FOLD)
                {
                    // if fold then mark player as inactive
                    players[currBettor].IsActive = false;
                }

                if(playersAction == eActionType.ACTION_RAISE)
                {
                    raisesRemaining--;
                    // if raise then update lastToAct to the preceding active player
                    lastToAct = GetPrevActivePlayer(currBettor);
                }

                if((playersAction == eActionType.ACTION_CALL) || (playersAction == eActionType.ACTION_RAISE))
                {
                    // if call or raise the take $ from players stack and put in pot
                    TransferMoneyToPot(currBettor, playersBetAmount);
                }

                BroadcastAction(stage, currBettor, playersAction, playersBetAmount);

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
            players[playerNum].StackSize -= amount;
            players[playerNum].BetsThisBettingRound += amount;
            if (players[playerNum].BetsThisBettingRound > maxBetsThisBettingRound)
            {
                maxBetsThisBettingRound = players[playerNum].BetsThisBettingRound;
            }
            potSize += amount;
        }

        private void Showdown(Card[] board, ref List<int> winningPlayers, int lastToAct)
        {
            int currPlayer = GetNextActivePlayer(lastToAct);
            int firstToAct = currPlayer;
            Hand overallBestHand; 

            // evaluate and show hand for first player to act - flag them as winning for now
            overallBestHand = Hand.FindPlayersBestHand(players[firstToAct].HoleCards(), board);
            BroadcastPlayerHand(firstToAct, overallBestHand);
            winningPlayers.Add(firstToAct);

            // Loop through other active players 
            currPlayer = GetNextActivePlayer(currPlayer);

            do
            {
                ServerHoldemPlayer player = players[currPlayer];
                eActionType playersAction;
                int playersAmount;

                // if not first to act then player may fold without showing cards
                player.GetAction(eStage.STAGE_SHOWDOWN, 0, 0, 0, 0, potSize, out playersAction, out playersAmount);

                if (playersAction == eActionType.ACTION_FOLD)
                {
                    BroadcastAction(eStage.STAGE_SHOWDOWN, currPlayer, playersAction, 0);
                }
                else
                {
                    Hand playerBestHand = Hand.FindPlayersBestHand(player.HoleCards(), board);
                    BroadcastPlayerHand(currPlayer, playerBestHand);

                    int result = Hand.compareHands(overallBestHand, playerBestHand);
                    if (result == 1)
                    {
                        // this hand is better than current best hand
                        winningPlayers.Clear();
                    }

                    if (result >= 0)
                    {
                        // this hand is equal to or better than current best hand
                        overallBestHand = playerBestHand;
                        winningPlayers.Add(player.PlayerNum);
                    }
                }

                currPlayer = GetNextActivePlayer(currPlayer);

            } while (currPlayer != firstToAct);
        }

        private void DistributeWinnings(List<int> winningPlayers)
        {
            int shareOfWinnings = potSize / winningPlayers.Count();

            // distribute winnings
            foreach (int i in winningPlayers)
            {
                players[i].StackSize += shareOfWinnings;
                potSize -= shareOfWinnings;
                BroadcastAction(eStage.STAGE_SHOWDOWN, i, eActionType.ACTION_WIN, shareOfWinnings);
            }
        }

        private void KillBrokePlayers()
        {
            // Kill off broke players
            foreach (ServerHoldemPlayer player in players)
            {
                if(player.IsAlive && player.StackSize <= 0)
                {
                    System.Console.WriteLine("Player {0} has been eliminated", player.PlayerNum);

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
    }
}
