using System;
using System.Collections;
using System.Collections.Generic;
using HoldemPlayerContract;

namespace HoldemController.ConsoleDisplay
{
    internal class DisplayManager
    {
        private readonly int _width;
        private readonly int _height;
        private readonly DrawRegion _regions;
        
        internal DisplayManager(int width, int height, int numPlayers)
        {
            Console.SetWindowSize(width, height);
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            _regions = new DrawRegion(width, height, numPlayers);

            _width = width;
            _height = height;
        }

        public void DrawTable()
        {
            var table = _regions.Table;
            var lines = new List<ConsoleLine>();
            for (var i = 0; i < table.Height; i++)
            {
                lines.Add(new ConsoleLine(table.X, table.Y + i, new string(' ', table.Width)));
            }
            DrawLines(lines, ConsoleColor.DarkGreen);
        }

        public void UpdateCommunityCards(Card[] cards)
        {
            var region = _regions.CommunityCards;
            DrawLines(BuildCards(cards, region.X, region.Y, true));
        }

        internal void UpdatePots(List<Pot> pots) 
        {
            // todo: needs updating using the Region stuffs

            var potWidth = 20;
            var x = _width / 2 - 30;
            var y = _height / 2 - 1;
            var potDisplay = new List<ConsoleLine>();
            var potNum = 1;
            foreach (var pot in pots)
            {
                potDisplay.Add(new ConsoleLine(x, y, ("Pot " + potNum + ": " + pot.Size()).PadRight(potWidth), ConsoleColor.White, ConsoleColor.DarkGreen));
                potNum++;
                y++;
            }
            for (var i = 0; i < 4 - pots.Count; i++)
            {
                potDisplay.Add(new ConsoleLine(x, y, new string(' ', potWidth), ConsoleColor.White, ConsoleColor.DarkGreen));
                potNum++;
                y++;
            }
            DrawLines(potDisplay);
        }

        public void UpdatePlayerAction(UiPlayer player, ActionType? act, int amount)
        {
            var actionRegion = _regions.PlayerAction[player.PlayerId];

            var consoleColor = ConsoleColor.Green;
            switch (act)
            {
                case ActionType.Check:
                case ActionType.Blind:
                case ActionType.Call:
                    consoleColor = ConsoleColor.Cyan;
                    break;
                case ActionType.Raise:
                    consoleColor = ConsoleColor.Yellow;
                    break;
                case ActionType.Fold:
                    consoleColor = ConsoleColor.Black;
                    break;
                case ActionType.Show:
                    break;
                case ActionType.Win:
                    consoleColor = ConsoleColor.Blue;
                    break;
            }

            var playerAction = new ConsoleLine(actionRegion.X, actionRegion.Y, actionRegion.FormatText(act?.ToString()), consoleColor);

            var bet = actionRegion.FormatText(((IList) new[] {ActionType.Blind, ActionType.Call, ActionType.Raise, ActionType.Win}).Contains(act) ? amount.ToString() : string.Empty);
            var betAmount = new ConsoleLine(actionRegion.X, actionRegion.Y + 1, bet, ConsoleColor.White);
            DrawLines(new List<ConsoleLine> {playerAction, betAmount}, ConsoleColor.DarkGreen);

            if (act == ActionType.Fold)
            {
                UpdateHoleCards(player.PlayerId, new Card[2], false);
            }
        }

        internal void UpdatePlayer(UiPlayer player)
        {
            var playerId = player.PlayerId;
            var playerRegion = _regions.Players[playerId];
            var name = new ConsoleLine(playerRegion.X, playerRegion.Y, playerRegion.FormatText(player.Name));
            var stackSize = new ConsoleLine(playerRegion.X, playerRegion.Y + 1, playerRegion.FormatText(player.StackSize.ToString()));
            DrawLines(new List<ConsoleLine> {name, stackSize}, ConsoleColor.Black, ConsoleColor.White);
            
            UpdateHoleCards(playerId, player.HoleCards, player.IsAlive);

            UpdateDealerChip(playerId, player.IsDealer);
        }
        
        public void ShowPlayerTurn(int? playerId)
        {
            foreach (var player in _regions.PlayerTurnIndicator)
            {
                var region = player.Value;
                var isPlayerTurn = player.Key == playerId;
                var text = region.FormatText(isPlayerTurn ? "..." : string.Empty);
                var turn = new ConsoleLine(region.X, region.Y, text);
                var bgColor = isPlayerTurn ? ConsoleColor.Black : ConsoleColor.DarkGreen;
                DrawLines(new List<ConsoleLine> { turn }, bgColor, ConsoleColor.White);
            }
        }

        private void UpdateHoleCards(int playerId, Card[] holeCards, bool isAlive)
        {
            var cardRegion = _regions.PlayerCards[playerId];
            var cards = BuildCards(holeCards, cardRegion.X, cardRegion.Y, isAlive);
            DrawLines(cards, ConsoleColor.White, ConsoleColor.Black);
        }

        private void UpdateDealerChip(int playerId, bool isDealer)
        {
            var region = _regions.PlayerDealerChip[playerId];
            var text = isDealer ? "(D)" : string.Empty;
            var dealerChip = new ConsoleLine(region.X, region.Y, region.FormatText(text));
            DrawLines(new [] { dealerChip }, ConsoleColor.DarkGreen, ConsoleColor.DarkRed);
        }

        private static List<ConsoleLine> BuildCards(Card[] holeCards, int x, int y, bool isAlive)
        {
            var cards = new List<ConsoleLine>();
            var xVal = x;
            if (!isAlive)
            {
                cards.AddRange(BlankCard(x, y, ConsoleColor.DarkGreen));
                cards.AddRange(BlankCard(x + 4, y, ConsoleColor.DarkGreen));
            }
            else
            {
                foreach (var card in holeCards)
                {
                    cards.AddRange(card != null ? Card(card.Rank, card.Suit, xVal, y) : BlankCard(xVal, y, ConsoleColor.DarkMagenta));
                    xVal += 4;
                }
            }
            return cards;
        }

        private static void DrawLines(IEnumerable<ConsoleLine> lines, ConsoleColor? bgColor = null, ConsoleColor? fgColor = null)
        {
            var bg = Console.BackgroundColor;
            var fg = Console.ForegroundColor;
            foreach (var line in lines)
            {
                Console.ForegroundColor = line.Color ?? fgColor ?? fg;
                Console.BackgroundColor = line.BackgroundColor ?? bgColor ?? fg;

                Console.SetCursorPosition(line.X, line.Y);
                Console.Write(line.Text);

                Console.BackgroundColor = bgColor ?? fg;
                Console.ForegroundColor = fgColor ?? fg;
            }
            Console.BackgroundColor = bg;
            Console.ForegroundColor = fg;
            Console.SetCursorPosition(0, 0);
        }

        private static IEnumerable<ConsoleLine> Card(ERankType rank, ESuitType suit, int startingX, int startingY)
        {
            var rankSymbol = GetRankSymbol(rank);
            var isDouble = rankSymbol.Length > 1;
            var cardColor = GetSuitColor(suit);
            return new List<ConsoleLine>
            {
                new ConsoleLine(startingX, startingY, rankSymbol + (isDouble ? " " : "  "), cardColor, ConsoleColor.White), new ConsoleLine(startingX, startingY + 1, " " + GetSuitSymbol(suit) + " ", cardColor, ConsoleColor.White), new ConsoleLine(startingX, startingY + 2, (isDouble ? " " : "  ") + rankSymbol, cardColor, ConsoleColor.White)
            };
        }

        private static IEnumerable<ConsoleLine> BlankCard(int startingX, int startingY, ConsoleColor color)
        {
            return new List<ConsoleLine>
            {
                new ConsoleLine(startingX, startingY, "   ", backgroundColor: color), new ConsoleLine(startingX, startingY + 1, "   ", backgroundColor: color), new ConsoleLine(startingX, startingY + 2, "   ", backgroundColor: color)
            };
        }

        private static ConsoleColor GetSuitColor(ESuitType suit)
        {
            if (suit == ESuitType.SuitDiamonds || suit == ESuitType.SuitHearts)
            {
                return ConsoleColor.Red;
            }
            return ConsoleColor.Black;
        }

        private static string GetSuitSymbol(ESuitType suit)
        {
            switch (suit)
            {
                case ESuitType.SuitClubs:
                    return "\u2663";
                case ESuitType.SuitHearts:
                    return "\u2665";
                case ESuitType.SuitSpades:
                    return "\u2660";
                case ESuitType.SuitDiamonds:
                    return "\u2666";
                case ESuitType.SuitUnknown:
                    return " ";
                default:
                    throw new ArgumentOutOfRangeException(nameof(suit), suit, null);
            }
        }

        private static string GetRankSymbol(ERankType rank)
        {
            switch (rank)
            {
                case ERankType.RankTwo:
                    return "2";
                case ERankType.RankThree:
                    return "3";
                case ERankType.RankFour:
                    return "4";
                case ERankType.RankFive:
                    return "5";
                case ERankType.RankSix:
                    return "6";
                case ERankType.RankSeven:
                    return "7";
                case ERankType.RankEight:
                    return "8";
                case ERankType.RankNine:
                    return "9";
                case ERankType.RankTen:
                    return "10";
                case ERankType.RankJack:
                    return "J";
                case ERankType.RankQueen:
                    return "Q";
                case ERankType.RankKing:
                    return "K";
                case ERankType.RankAce:
                    return "A";
                case ERankType.RankUnknown:
                    return " ";
                default:
                    throw new ArgumentOutOfRangeException(nameof(rank), rank, null);
            }
        }
    }
}