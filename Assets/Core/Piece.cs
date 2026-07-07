namespace Othello.Core
{
    /// <summary>
    /// Color constants for the contents of a board square. Inside the engine a player is
    /// identified by the color of their pieces, so engine APIs (GetCurrentPlayer, GetWinner,
    /// GetPieceCount) use these values for players as well. App and UI code referring to a
    /// player should use the matching Othello.App.Player constants instead.
    /// </summary>
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