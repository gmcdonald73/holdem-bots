using System.Collections.Generic;
using HoldemPlayerContract;

namespace HoldemController.ConsoleDisplay
{
    internal class ConsoleRenderer : IEventHandler
    {
        private DisplayManager _display;
        private Dictionary<int, UiPlayer> _players;

        public void Initialise(List<PlayerInfo> players)
        {
            _display = new DisplayManager(150, 35, players.Count); // todo: shouldn't have to set the width + height here. this is a UI element

            _display.DrawTable();
            _players = new Dictionary<int, UiPlayer>(players.Count);
            foreach (var player in players)
            {
                var playerId = player.PlayerNum;
                var uiPlayer = new UiPlayer(playerId, player.Name, player.IsAlive, player.StackSize);
                _players.Add(playerId, uiPlayer);

                _display.UpdatePlayer(uiPlayer);
            }
        }

        public void BeginHand(int dealerPlayerId)
        {
            // todo: do we update which players are no longer in the hand?
            foreach (var player in _players.Values)
            {
                player.IsDealer = dealerPlayerId == player.PlayerId;
                _display.UpdatePlayer(player);
            }
        }

        public void TakeBlinds(int playerId, int amount)
        {
            var player = _players[playerId];
            player.StackSize -= amount;
            player.TotalStageBet = amount;
            _display.UpdatePlayer(player);
            _display.UpdatePlayerAction(player, ActionType.Blind, amount);
        }

        public void BeginStage(Stage stage, Card[] cards)
        {
            _display.UpdateCommunityCards(cards);
        }

        public void AwaitingPlayerAction(int playerId)
        {
            _display.ShowPlayerTurn(playerId);
        }

        public void DealHand(int playerId, Card card1, Card card2)
        {
            var player = _players[playerId];
            player.HoleCards[0] = card1; // todo: is this the easiest way to store this?
            player.HoleCards[1] = card2;
        }

        public void PlayerActionPerformed(int playerId, int stackSize, ActionType action, int betAmount)
        {
            var player = _players[playerId];
            player.StackSize = stackSize;
            player.TotalStageBet = betAmount;
            
            _display.UpdatePlayer(player);
            _display.UpdatePlayerAction(player, action, betAmount);
        }

        public void EndStage(List<Pot> pots)
        {
            _display.UpdatePots(pots);
            foreach (var player in _players.Values)
            {
                player.TotalStageBet = 0;
                _display.UpdatePlayer(player);
                _display.UpdatePlayerAction(player, null, 0);
            }
            _display.ShowPlayerTurn(null);
        }

        public void DistributeWinnigs(IDictionary<int, int> playerWinnings)
        {
            foreach (var winnings in playerWinnings)
            {
                var player = _players[winnings.Key];
                var amount = winnings.Value;
                player.StackSize += amount;
                _display.UpdatePlayerAction(player, ActionType.Win, amount);
            }
            _display.UpdatePots(new List<Pot>());
        }

        public void EndHand()
        {
            foreach (var player in _players.Values)
            {
                player.HoleCards[0] = null;
                player.HoleCards[1] = null;
                _display.UpdatePlayer(player);
                _display.UpdatePlayerAction(player, null, 0);
            }
        }

        public void EndGame()
        {
            
        }
    }
}