namespace Othello.Core
{
    public static class Piece
    {
        public const byte Empty = 0b00;
        public const byte Black = 0b01;
        public const byte White = 0b10;

        private const int m_Mask = 0b11;

        public static bool IsBlack(int piece)
        {
            return (piece & m_Mask) == Black;
        }

        public static string GetPlayerAsString(int playerColor)
        {
            return playerColor == Piece.White ? "White" : "Black";
        }

    }
}