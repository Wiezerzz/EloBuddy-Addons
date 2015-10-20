using System.Drawing;
using EloBuddy;

namespace wzUtility
{
    class DrawingHelper
    {
        public static void DrawRectangle(float X, float Y, float Width, float Height, Color Color)
        {
            Drawing.DrawLine(X, Y, X + Width, Y, 1, Color); // Top
            Drawing.DrawLine(X + Width - 1, Y + 1, X + Width - 1, Y + Height - 1, 1, Color); // Right
            Drawing.DrawLine(X, Y + Height - 1, X + Width, Y + Height - 1, 1, Color); // Bottom
            Drawing.DrawLine(X, Y + 1, X, Y + Height - 1, 1, Color); // Left
        }

        //TODO: Improve this. (performance)
        public static void DrawFilledRectangle(float X, float Y, float Width, float Height, Color color)
        {
            for (int i = 0; i < Height; i++)
            {
                Drawing.DrawLine(X, Y + i, X + Width, Y + i, 1, color);
            }
        }
    }
}
