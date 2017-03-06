using System.Collections.Generic;
using HoldemPlayerContract;

namespace HoldemController.ConsoleDisplay
{
    internal class GraphicsDisplay : IDisplay
    {
        private DisplayManager _display;
        private Card[] _board;
        private int _boardCardNum;
        private Dictionary<int, UiPlayer> _players;
        private int _sleepAfterActionMilliSeconds;

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
            if (handNum == 1) // todo: should be doing this at game start, not hand init
            {
                _players = new Dictionary<int, UiPlayer>(players.Count);
                foreach (var player in players)
                {
                    _players.Add(player.PlayerNum, new UiPlayer(player.PlayerNum, player.Name, player.IsAlive, player.StackSize));
                }
            }
            else
            {
                foreach (var player in players)
                {
                    var uiPlayer = _players[player.PlayerNum];
                    uiPlayer.IsAlive = player.IsAlive;
                    uiPlayer.IsActive = player.IsAlive; // if alive then player is active at start of hand
                    uiPlayer.TotalStageBet = 0;
                    uiPlayer.StackSize = player.StackSize;
                }
            }
            foreach (var player in _players.Values)
            {
                _display.UpdatePlayer(player); // clears previous rounds cards 
            }
        }

        public void DisplayHoleCards(int playerId, Card hole1, Card hole2)
        {
            var player = _players[playerId];

            player.HoleCards[0] = hole1;
            player.HoleCards[1] = hole2;

            _display.UpdatePlayer(player);

            if(_sleepAfterActionMilliSeconds > 0)
            {
                System.Threading.Thread.Sleep(_sleepAfterActionMilliSeconds);
            }
        }

        public void DisplayAction(Stage stage, int playerId, ActionType action, int totalAmount, int betSize, int callAmount, int raiseAmount, bool isAllIn, PotManager potMan)
        {
            UiPlayer player = _players[playerId];

            if(action == ActionType.Call || action == ActionType.Raise || action == ActionType.Blind)
            {
                player.StackSize -= totalAmount;
            }
            else if(action == ActionType.Win)
            {
                player.StackSize += totalAmount;
            }

            _display.UpdatePlayer(player);
            _display.UpdatePlayerAction(player, action, betSize);
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
