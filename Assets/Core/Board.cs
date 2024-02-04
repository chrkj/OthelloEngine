using System;
using System.Collections.Generic;

namespace Othello.Core
{
    public class Board
    {
        public const int MAX_LEGAL_MOVES = 30;

        private ulong m_BlackPieces;
        private ulong m_WhitePieces;
        private bool m_IsWhiteToMove;

        /// <summary>
        /// Creates a deep copy of the current board.
        /// </summary>
        /// <returns>The copied board.</returns>
        public Board Copy()
        {
            var copy = new Board
            {
                m_BlackPieces = m_BlackPieces,
                m_IsWhiteToMove = m_IsWhiteToMove,
                m_WhitePieces = m_WhitePieces
            };
            return copy;
        }

        /// <summary>
        /// Resets the state of the game board.
        /// </summary>
        /// <param name="playerToStart">The player to start the next game.</param>
        public void ResetBoard(int playerToStart)
        {
            m_BlackPieces = 0;
            m_WhitePieces = 0;
            m_IsWhiteToMove = playerToStart == Piece.WHITE;
        }

        /// <summary>
        /// Determines if the specified index on the board is empty.
        /// </summary>
        /// <param name="index">The index on the board to check.</param>
        /// <returns>True if the specified index is empty; otherwise, false.</returns>
        public bool IsEmpty(int index)
        {
            return ((m_BlackPieces | m_WhitePieces) & (1UL << index)) == 0;
        }

        /// <summary>
        /// Checks if the current board state is a terminal state.
        /// </summary>
        /// <returns>Returns true if the board state is terminal, false otherwise.</returns>
        public bool IsTerminalBoardState()
        {
            // Check if board is full (i.e. all bits are set)
            if ((m_BlackPieces | m_WhitePieces) == 0xFFFFFFFFFFFFFFFF)
                return true;

            Span<Move> legalMovesCurrentPlayer = stackalloc Move[256];
            GenerateLegalMoves(ref legalMovesCurrentPlayer);
            var numLegalMovesCurrentPlayer = legalMovesCurrentPlayer.Length;
            if (numLegalMovesCurrentPlayer != 0)
                return false;

            ChangePlayerToMove();
            Span<Move> legalMovesCurrentOpponent = stackalloc Move[256];
            GenerateLegalMoves(ref legalMovesCurrentOpponent);
            var numLegalMovesCurrentOpponent = legalMovesCurrentOpponent.Length;
            ChangePlayerToMove();

            return numLegalMovesCurrentOpponent == 0;
        }

        /// <summary>
        /// Make a move on the game board.
        /// </summary>
        /// <param name="move">The move to be made.</param>
        public void MakeMove(Move move)
        {
            PlacePieces(move);
        }

        /// <summary>
        /// Determines whether the current board is equal to another board.
        /// </summary>
        /// <param name="other">The board to compare with the current board.</param>
        /// <returns>True if the two boards are equal; otherwise, false.</returns>
        public bool Equals(Board other)
        {
            return m_WhitePieces == other.m_WhitePieces && m_BlackPieces == other.m_BlackPieces &&
                   m_IsWhiteToMove == other.m_IsWhiteToMove;
        }

        /// <summary>
        /// Generates the legal moves for the current board state and stores them in the provided span.
        /// </summary>
        /// <param name="legalMoves">The span to store the generated legal moves.</param>
        public void GenerateLegalMoves(ref Span<Move> legalMoves)
        {
            var legalMoveBitboard = GenerateLegalMoveBitboard();
            int currIndexPtr = 0;
            for (int i = 0; i < 64; i++)
            {
                ulong mask = (ulong)1 << i;
                if ((legalMoveBitboard & mask) != 0)
                    legalMoves[currIndexPtr++] = new Move(i);
            }

            legalMoves = legalMoves.Slice(0, currIndexPtr);
        }


        /// <summary>
        /// Loads the starting position of the game board.
        /// </summary>
        /// <param name="/*END_USER_CODE*/">No additional parameters required.</param>
        public void LoadStartPosition()
        {
            PlacePiece(27, Piece.BLACK);
            PlacePiece(28, Piece.WHITE);
            PlacePiece(35, Piece.WHITE);
            PlacePiece(36, Piece.BLACK);
        }


        /// <summary>
        /// Changes the player to move. If the current player is white, it changes it to black,
        /// and if the current player is black, it changes it to white.
        /// </summary>
        public void ChangePlayerToMove()
        {
            m_IsWhiteToMove = !m_IsWhiteToMove;
        }

        /// <summary>
        /// Gets the color of the piece at the specified position on the game board.
        /// </summary>
        /// <param name="file">The file of the position, ranging from 0 to 7 inclusive.</param>
        /// <param name="rank">The rank of the position, ranging from 0 to 7 inclusive.</param>
        /// <returns>The color of the piece at the specified position. Returns 0 if the position is empty, 1 if the piece is black, and 2 if the piece is white.</returns>
        public int GetPieceColor(int file, int rank)
        {
            var index = GetIndex(file, rank);
            if (IsEmpty(index))
                return 0;
            return (m_BlackPieces & (1UL << index)) == 0 ? Piece.WHITE : Piece.BLACK;
        }

        /// <summary>
        /// Gets the current player.
        /// </summary>
        /// <returns>The current player. Returns Player.WHITE if it's the white player's turn, otherwise returns Player.BLACK.</returns>
        public int GetCurrentPlayer()
        {
            return m_IsWhiteToMove ? Player.WHITE : Player.BLACK;
        }

        /// <summary>
        /// Gets the current player as a string.
        /// </summary>
        /// <returns>The current player as a string. Returns "White" if it is white player's turn, "Black" otherwise.</returns>
        public string GetCurrentPlayerAsString()
        {
            return m_IsWhiteToMove ? "White" : "Black";
        }

        /// <summary>
        /// Retrieves the number of pieces owned by the specified player on the board.
        /// </summary>
        /// <param name="player">The player for which to count the pieces. Use the constants from the Piece class (Piece.BLACK or Piece.WHITE).</param>
        /// <returns>The number of pieces owned by the specified player.</returns>
        public int GetPieceCount(int player)
        {
            var count = 0;
            for (var i = 0; i < 64; i++)
                if (!IsEmpty(i) && GetPieceColor(i) == player)
                    count++;
            return count;
        }

        /// <summary>
        /// Get the current state of the board.
        /// </summary>
        /// <returns>
        /// Returns the board state:
        /// -1: If the game is not yet over.
        /// 0: If the game ends in a draw.
        /// 1: If the BLACK player wins.
        /// 2: If the WHITE player wins.
        /// </returns>
        public int GetBoardState()
        {
            if (!IsTerminalBoardState()) return -1;
            return GetWinner();
        }

        /// <summary>
        /// Retrieves the positions of the pieces for the specified player.
        /// </summary>
        /// <param name="player">The player for whom to retrieve the positions of the pieces.</param>
        /// <returns>A list of integers representing the positions of the pieces.</returns>
        public List<int> GetPieces(int player)
        {
            var positions = new List<int>();
            var pieces = (player == Player.BLACK) ? m_BlackPieces : m_WhitePieces;
            for (int i = 0; i < 64; i++)
                if ((pieces & (1UL << i)) != 0)
                    positions.Add(i);
            return positions;
        }

        /// <summary>
        /// Calculates the hash value of the board.
        /// </summary>
        /// <returns>The hash value of the board.</returns>
        public ulong GetHash()
        {
            ulong hash = 5648423;
            if (m_IsWhiteToMove)
                hash = 4239784;
            return m_BlackPieces ^ hash;
        }

        /// <summary>
        /// Gets a bitboard representing all the pieces on the board.
        /// </summary>
        /// <returns>A ulong value representing the bitboard.</returns>
        public ulong GetAllPieces()
        {
            return (m_BlackPieces | m_WhitePieces);
        }

        /// <summary>
        /// Gets the winner of the game.
        /// </summary>
        /// <returns>The winner of the game. Returns 0 for draw, 1 for black, and 2 for white.</returns>
        public int GetWinner()
        {
            var blackPieceCount = GetPieceCount(Piece.BLACK);
            var whitePieceCount = GetPieceCount(Piece.WHITE);
            if (blackPieceCount == whitePieceCount) return 0;
            return blackPieceCount > whitePieceCount ? Piece.BLACK : Piece.WHITE;
        }

        /// <summary>
        /// Gets the index of a position on the game board given the file and rank.
        /// </summary>
        /// <param name="file">The file of the position, ranging from 0 to 7 inclusive.</param>
        /// <param name="rank">The rank of the position, ranging from 0 to 7 inclusive.</param>
        /// <returns>The index of the position on the game board.</returns>
        public static int GetIndex(int file, int rank)
        {
            return rank * 8 + file;
        }

        /// <summary>
        /// Determines if the specified file and rank are out of bounds.
        /// </summary>
        /// <param name="file">The file (column) index.</param>
        /// <param name="rank">The rank (row) index.</param>
        /// <returns>True if the file and rank are out of bounds, false otherwise.</returns>
        public static bool IsOutOfBounds(int file, int rank)
        {
            return file < 0 | file > 7 | rank < 0 | rank > 7;
        }

        /// <summary>
        /// Converts an index to a bit mask.
        /// </summary>
        /// <param name="id">The index to convert.</param>
        /// <returns>The bit mask representing the index.</returns>
        public static ulong IndexToBit(int id)
        {
            ulong mask = 0x8000000000000000;
            int x = id >> 3;
            int y = id & 7;
            mask >>= y;
            mask >>= (x * 8);
            return mask;
        }

        /// <summary>
        /// Places a piece on the game board.
        /// </summary>
        /// <param name="index">The index on the board where the piece will be placed.</param>
        /// <param name="player">The player who owns the piece.</param>
        private void PlacePiece(int index, int player)
        {
            switch (player)
            {
                case Piece.BLACK:
                    m_BlackPieces |= 1UL << index;
                    m_WhitePieces &= ~(1UL << index);
                    break;
                case Piece.WHITE:
                    m_WhitePieces |= 1UL << index;
                    m_BlackPieces &= ~(1UL << index);
                    break;
            }
        }

        /// <summary>
        /// Gets the color of the piece at the specified position on the board.
        /// </summary>
        /// <param name="index">The index of the position on the board.</param>
        /// <returns>
        /// The color of the piece at the specified position on the board.
        /// Returns 0 for an empty position, 1 for a black piece, and 2 for a white piece.
        /// </returns>
        private int GetPieceColor(int index)
        {
            if (IsEmpty(index))
                return 0;
            return (m_BlackPieces & (1UL << index)) == 0 ? Piece.WHITE : Piece.BLACK;
        }

        /// <summary>
        /// Places the captured pieces on the board based on the given move.
        /// </summary>
        /// <param name="move">The move to be played.</param>
        private void PlacePieces(Move move)
        {
            var index = 63 - move.Index;
            ulong opponentBitBoard;
            ulong currentPlayerBitboard;
            if (m_IsWhiteToMove)
            {
                opponentBitBoard = m_BlackPieces;
                currentPlayerBitboard = m_WhitePieces;
            }
            else
            {
                opponentBitBoard = m_WhitePieces;
                currentPlayerBitboard = m_BlackPieces;
            }

            if (0 <= index && index < 64)
            {
                ulong bit = IndexToBit(index);
                ulong rev = 0;

                for (int dir = 0; dir < 8; ++dir)
                {
                    ulong rev_ = 0;
                    ulong mask = MoveDir(bit, dir);

                    while ((mask != 0) && ((mask & opponentBitBoard) != 0))
                    {
                        rev_ |= mask;
                        mask = MoveDir(mask, dir);
                    }

                    if ((mask & currentPlayerBitboard) != 0)
                        rev |= rev_;
                }

                if (m_IsWhiteToMove)
                {
                    m_BlackPieces ^= rev;
                    m_WhitePieces ^= (bit | rev);
                }
                else
                {
                    m_WhitePieces ^= rev;
                    m_BlackPieces ^= (bit | rev);
                }
            }
        }

        /// <summary>
        /// Generates a bitboard representing the legal moves that the current player can make.
        /// </summary>
        /// <returns>A ulong representing the legal moves bitboard.</returns>
        /// <remarks>
        /// The legal moves bitboard is a binary representation where each bit represents a square on the board.
        /// A bit value of 1 indicates that the current player can make a legal move to that square, while a bit value of 0 indicates an illegal move.
        /// This method calculates the legal moves bitboard based on the current board state.
        /// </remarks>
        private ulong GenerateLegalMoveBitboard()
        {
            ulong opponentBitBoard;
            ulong currentPlayerBitboard;
            if (m_IsWhiteToMove)
            {
                opponentBitBoard = m_BlackPieces;
                currentPlayerBitboard = m_WhitePieces;
            }
            else
            {
                opponentBitBoard = m_WhitePieces;
                currentPlayerBitboard = m_BlackPieces;
            }

            ulong blankBoard = ~(m_BlackPieces | m_WhitePieces);
            ulong allEdgeBitmap = opponentBitBoard & 0x007e7e7e7e7e7e00;
            ulong verticalEdgeBitmap = opponentBitBoard & 0x00FFFFFFFFFFFF00;
            ulong horizontalEdgeBitmap = opponentBitBoard & 0x7e7e7e7e7e7e7e7e;

            var tmp = horizontalEdgeBitmap & (currentPlayerBitboard << 1);
            for (int i = 0; i < 5; ++i)
                tmp |= horizontalEdgeBitmap & (tmp << 1);
            var legalMovesBitBoard = blankBoard & (tmp << 1);

            tmp = horizontalEdgeBitmap & (currentPlayerBitboard >> 1);
            for (int i = 0; i < 5; ++i)
                tmp |= horizontalEdgeBitmap & (tmp >> 1);
            legalMovesBitBoard |= blankBoard & (tmp >> 1);

            tmp = verticalEdgeBitmap & (currentPlayerBitboard << 8);
            for (int i = 0; i < 5; ++i)
                tmp |= verticalEdgeBitmap & (tmp << 8);
            legalMovesBitBoard |= blankBoard & (tmp << 8);

            tmp = verticalEdgeBitmap & (currentPlayerBitboard >> 8);
            for (int i = 0; i < 5; ++i)
                tmp |= verticalEdgeBitmap & (tmp >> 8);
            legalMovesBitBoard |= blankBoard & (tmp >> 8);

            tmp = allEdgeBitmap & (currentPlayerBitboard << 7);
            for (int i = 0; i < 5; ++i)
                tmp |= allEdgeBitmap & (tmp << 7);
            legalMovesBitBoard |= blankBoard & (tmp << 7);

            tmp = allEdgeBitmap & (currentPlayerBitboard << 9);
            for (int i = 0; i < 5; ++i)
                tmp |= allEdgeBitmap & (tmp << 9);
            legalMovesBitBoard |= blankBoard & (tmp << 9);

            tmp = allEdgeBitmap & (currentPlayerBitboard >> 9);
            for (int i = 0; i < 5; ++i)
                tmp |= allEdgeBitmap & (tmp >> 9);
            legalMovesBitBoard |= blankBoard & (tmp >> 9);

            tmp = allEdgeBitmap & (currentPlayerBitboard >> 7);
            for (int i = 0; i < 5; ++i)
                tmp |= allEdgeBitmap & (tmp >> 7);
            legalMovesBitBoard |= blankBoard & (tmp >> 7);

            return legalMovesBitBoard;
        }


        /// <summary>
        /// Moves in a specified direction on the game board.
        /// </summary>
        /// <param name="id">The bit representing the piece to move.</param>
        /// <param name="dir">The direction in which to move.</param>
        /// <returns>The bit representing the moved piece.</returns>
        private ulong MoveDir(ulong id, int dir)
        {
            return dir switch
            {
                0 => (id << 8) & 0xffffffffffffff00,
                1 => (id << 7) & 0x7f7f7f7f7f7f7f00,
                2 => (id >> 1) & 0x7f7f7f7f7f7f7f7f,
                3 => (id >> 9) & 0x007f7f7f7f7f7f7f,
                4 => (id >> 8) & 0x00ffffffffffffff,
                5 => (id >> 7) & 0x00fefefefefefefe,
                6 => (id << 1) & 0xfefefefefefefefe,
                7 => (id << 9) & 0xfefefefefefefe00,
                _ => 0
            };
        }
    }
}