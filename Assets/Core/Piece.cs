namespace Othello.Core
{
    public static class Piece
    {
        public const byte Empty = 0b00;
        public const byte Black = 0b01;
        public const byte White = 0b10;

        private const byte Mask = 0b11;

        public static bool IsBlack(int piece)
        {
            return (piece & Mask) == Black;
        }

        public static bool IsSameColor(int piece1, int piece2)
        {
            return (piece1 & Mask) == (piece2 & Mask);
        }

    }
}