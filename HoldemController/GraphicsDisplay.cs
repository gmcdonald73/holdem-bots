using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HoldemPlayerContract;

namespace HoldemController
{
    internal class UIPlayer
    {
        public int PlayerNum {get; set; }
        public string Name  {get; set; }
        public bool IsAlive  {get; set; }
        public bool IsActive  {get; set; }
        public int StackSize  {get; set; }
        public int BetsThisHand  {get; set; }

        public Card[] HoleCards;

        public UIPlayer(int pPlayerNum, string pName, bool pIsAlive, int pStackSize)
        {
            PlayerNum = pPlayerNum;
            Name = pName;
            IsAlive = pIsAlive;
            IsActive = pIsAlive;
            StackSize = pStackSize;
            BetsThisHand = 0;
            HoleCards = new Card[2];
        }
    }

    internal class GraphicsDisplay : IDisplay
    {
        private DisplayManager _display;
        private Card [] _board;
        private int _boardCardNum = 0;
        private UIPlayer [] _players;
        private int _sleepAfterActionMilliSeconds = 0;

        public void Initialise(GameConfig gameConfig, int numPlayers, int sleepAfterActionMilliSeconds) 
        {
            _display = new DisplayManager(150, 35, numPlayers);
            _display.DrawTable();
            _sleepAfterActionMilliSeconds = sleepAfterActionMilliSeconds;
        }

        public void InitHand(int handNum, int numPlayers, List<PlayerInfo> players, int dealerId, int littleBlindSize, int bigBlindSize)
        {
            // Clear board
            _board = new Card[5];
            _boardCardNum = 0;
            _display.UpdateCommunityCards(_board);

            // create or update player profiles
            if (handNum == 1)
            {
                _players = new UIPlayer[numPlayers];

                foreach (PlayerInfo p in players)
                {
                    _players[p.PlayerNum] = new UIPlayer(p.PlayerNum, p.Name, p.IsAlive, p.StackSize);
                }
            }
            else
            {
                foreach (PlayerInfo p in players)
                {
                    _players[p.PlayerNum].IsAlive = p.IsAlive;
                    _players[p.PlayerNum].IsActive = p.IsAlive; // if alive then player is active at start of hand
                    _players[p.PlayerNum].BetsThisHand = 0;
                    _players[p.PlayerNum].StackSize = p.StackSize;
                }
            }
        }

        public void DisplayHoleCards(int playerId, Card hole1, Card hole2)
        {
            UIPlayer player = _players[playerId];

            player.HoleCards[0] = hole1;
            player.HoleCards[1] = hole2;

            _display.UpdatePlayer(player);

            if(_sleepAfterActionMilliSeconds > 0)
            {
                System.Threading.Thread.Sleep(_sleepAfterActionMilliSeconds);
            }
        }

        public void DisplayAction(EStage stage, int playerId, EActionType action, int totalAmount, int callAmount, int raiseAmount, bool isAllIn, PotManager potMan)
        {
            UIPlayer player = _players[playerId];

            if(action == EActionType.ActionCall || action == EActionType.ActionRaise || action == EActionType.ActionBlind)
            {
                player.StackSize -= totalAmount;
            }
            else if(action == EActionType.ActionWin)
            {
                player.StackSize += totalAmount;
            }

            _display.UpdatePlayer(player);
            _display.UpdatePlayerAction(player.IsAlive, playerId, action, totalAmount);
            _display.UpdatePots(potMan.Pots);

            if(_sleepAfterActionMilliSeconds > 0)
            {
                System.Threading.Thread.Sleep(_sleepAfterActionMilliSeconds);
            }
        }

        public void DisplayBoardCard(EBoardCardType cardType, Card boardCard)
        {
            _board[_boardCardNum] = boardCard;
            _display.UpdateCommunityCards(_board);
            _boardCardNum++;

            if(_sleepAfterActionMilliSeconds > 0)
            {
                System.Threading.Thread.Sleep(_sleepAfterActionMilliSeconds);
            }
        }

        public void DisplayPlayerHand(int playerNum, Card hole1, Card hole2, Hand bestHand)
        {
        }
        public void DisplayShowdown(HandRanker handRanker, PotManager potMan)
        {

        }

        public void DisplayEndOfGame(int numPlayers, List<PlayerInfo> players)
        {
        }
    }
}
