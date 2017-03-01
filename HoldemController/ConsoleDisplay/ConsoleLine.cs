using System;

namespace HoldemController.ConsoleDisplay
{
    public class ConsoleLine
    {
        public ConsoleLine(int x, int y, string text, ConsoleColor? color = null, ConsoleColor? backgroundColor = null)
        {
            X = x;
            Y = y;
            Text = text;
            Color = color;
            BackgroundColor = backgroundColor;
        }
        public int X { get; set; }
        public int Y { get; set; }
        public string Text { get; set; }
        public ConsoleColor? Color { get; set; }
        public ConsoleColor? BackgroundColor { get; set; }
    }
}