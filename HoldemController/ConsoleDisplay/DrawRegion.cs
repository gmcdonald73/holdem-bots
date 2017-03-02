using System;
using System.Collections.Generic;

namespace HoldemController.ConsoleDisplay
{
    public class DrawRegion
    {
        private const int PlayerWidth = 10;
        private const int PlayerHeight = 2; // todo: calculated?
        //private const int MaxMoneyWidth = 10;
        private const int CardWidth = 3;
        private const int HoldCardWidth = CardWidth*2 + 1;
        private const int CardHeight = 3;
        private const int CommunityCardsWidth = CardWidth*5 + 4;

        private const int ActionWidth = 10;
        private const int ActionHeight = 2;

        private const int HorizontalPadding = 1;
        private const int TableBorderWidth = HorizontalPadding + PlayerWidth + HorizontalPadding;
        private const int VerticalPadding = 1;
        private const int TableBorderHeight = VerticalPadding + PlayerHeight + VerticalPadding;


        public DrawRegion(int width, int height, int playerCount)
        {
            if (playerCount > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(playerCount), "Too many players. Maximum is 8.");
            }
            var tableWidth = width - (2*TableBorderWidth); // todo: validate min width
            var tableHeight = height - (2*TableBorderHeight); // todo: validate min height

            Table = new DrawArea(TableBorderWidth, TableBorderHeight, tableWidth, tableHeight);
            var yMid = height / 2 - 1;
            CommunityCards = new DrawArea((width - CommunityCardsWidth) / 2, yMid, CommunityCardsWidth, CardHeight );

            for (var i = 0; i < playerCount; i++)
            {
                int playerY;
                if (i == 0 || i == 4)
                {
                    playerY = yMid;
                }
                else if (i < 4)
                {
                    playerY = VerticalPadding;
                }
                else
                {
                    playerY = height - VerticalPadding - PlayerHeight;
                }
                int playerX;
                if (i == 0)
                {
                    playerX = HorizontalPadding;
                }
                else if (i == 4)
                {
                    playerX = width - HorizontalPadding - PlayerWidth;
                }
                else
                {
                    var playerWidth = tableWidth/4;
                    var leftMost = TableBorderWidth + playerWidth - PlayerWidth/2;
                    playerX = leftMost + (3 - Math.Abs(4 - i))* playerWidth;
                }
                Players.Add(i, new DrawArea(playerX, playerY, PlayerWidth, PlayerHeight));

                var cardX = playerX;
                var cardY = playerY;
                if (i == 0)
                {
                    cardX = TableBorderWidth + HorizontalPadding;
                }
                else if (i == 4)
                {
                    cardX = width - (TableBorderWidth + HorizontalPadding + HoldCardWidth);
                }
                else if (i < 4)
                {
                    cardY = TableBorderHeight + VerticalPadding;
                }
                else
                {
                    cardY = height - (TableBorderHeight + VerticalPadding + CardHeight);
                }

                PlayerCards.Add(i, new DrawArea(cardX, cardY, HoldCardWidth, CardHeight));

                var actionX = cardX;
                var actionY = cardY;
                if (i == 0)
                {
                    actionX = TableBorderWidth + HoldCardWidth + HorizontalPadding + HorizontalPadding;
                }
                else if (i == 4)
                {
                    actionX = width - (TableBorderWidth + HorizontalPadding + HoldCardWidth + HorizontalPadding + ActionWidth);
                }
                else if (i < 4)
                {
                    actionY = cardY + CardHeight + VerticalPadding;
                }
                else
                {
                    actionY = cardY - (CardHeight + VerticalPadding);
                }
                PlayerAction.Add(i, new DrawArea(actionX, actionY, ActionWidth, ActionHeight));
            }
        }

        public DrawArea Table { get; }
        public DrawArea CommunityCards { get; }
        public Dictionary<int, DrawArea> Players { get; } = new Dictionary<int,DrawArea>();
        public Dictionary<int, DrawArea> PlayerCards { get; } = new Dictionary<int, DrawArea>();
        public Dictionary<int, DrawArea> PlayerAction { get; } = new Dictionary<int, DrawArea>();

        public class DrawArea
        {
            public DrawArea(int x, int y, int width, int height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public int X { get; }
            public int Y { get; }
            public int Width { get; }
            public int Height { get; }

            public string FormatText(string text)
            {
                text = text ?? string.Empty;
                return text.Substring(0, Math.Min(text.Length, Width)).PadRight(Width);
            }
        }
    }
}