namespace Othello.Core
{
    public static class Piece
    {
        public const byte EMPTY = 0b00;
        public const byte BLACK = 0b01;
        public const byte WHITE = 0b10;

        private const int MASK = 0b11;

        public static bool IsBlack(int piece)
        {
            return (piece & MASK) == BLACK;
        }

    }
}