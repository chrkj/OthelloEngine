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

        public void ResetBoard(int playerToStart)
        {
            m_BlackPieces = 0;
            m_WhitePieces = 0;
            m_IsWhiteToMove = playerToStart == Piece.WHITE;
        }

        public bool IsEmpty(int index)
        {
            return ((m_BlackPieces | m_WhitePieces) & (1UL << index)) == 0;
        }

        public bool IsTerminalBoardState()
        {
            // Check if board is full (i.e. all bits are set)
            if ((m_BlackPieces | m_WhitePieces) == 0xFFFFFFFFFFFFFFFF)
                return true;

            Span<Move> legalMovesCurrentPlayer = stackalloc Move[256];
            GenerateLegalMovesStack(ref legalMovesCurrentPlayer);
            var numLegalMovesCurrentPlayer = legalMovesCurrentPlayer.Length;
            if (numLegalMovesCurrentPlayer != 0)
                return false;

            ChangePlayerToMove();
            Span<Move> legalMovesCurrentOpponent = stackalloc Move[256];
            GenerateLegalMovesStack(ref legalMovesCurrentOpponent);
            var numLegalMovesCurrentOpponent = legalMovesCurrentOpponent.Length;
            ChangePlayerToMove();

            return numLegalMovesCurrentOpponent == 0;
        }

        public void MakeMove(Move move)
        {
            PlacePieces(move);
        }

        public bool Equals(Board other)
        {
            return m_WhitePieces == other.m_WhitePieces && m_BlackPieces == other.m_BlackPieces && m_IsWhiteToMove == other.m_IsWhiteToMove;
        }

        public void GenerateLegalMovesStack(ref Span<Move> legalMoves)
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

        public void LoadStartPosition()
        {
            PlacePiece(27, Piece.BLACK);
            PlacePiece(28, Piece.WHITE);
            PlacePiece(35, Piece.WHITE);
            PlacePiece(36, Piece.BLACK);
        }

        public static bool IsOutOfBounds(int file, int rank)
        {
            return file < 0 | file > 7 | rank < 0 | rank > 7;
        }

        public void ChangePlayerToMove()
        {
            m_IsWhiteToMove = !m_IsWhiteToMove;
        }
        
        public static int GetIndex(int file, int rank)
        {
            return rank * 8 + file;
        }
        
        public int GetPieceColor(int file, int rank)
        {
            var index = GetIndex(file, rank);
            if (IsEmpty(index))
                return 0;
            return (m_BlackPieces & (1UL << index)) == 0 ? Piece.WHITE : Piece.BLACK;
        }
        
        public int GetCurrentPlayer()
        {
            return m_IsWhiteToMove ? Piece.WHITE : Piece.BLACK;
        }

        public string GetCurrentPlayerAsString()
        {
            return m_IsWhiteToMove ? "White" : "Black";
        }

        public int GetPieceCount(int player)
        {
            var count = 0;
            for (var i = 0; i < 64; i++)
                if (!IsEmpty(i) && GetPieceColor(i) == player)
                    count++;
            return count;
        }

        public int GetBoardState()
        {
            if (!IsTerminalBoardState()) return -1;
            return GetWinner();
        }
        
        public List<int> GetPieces(int player)
        {
            var positions = new List<int>();
            var pieces = (player == Player.BLACK) ? m_BlackPieces : m_WhitePieces;
            for (int i = 0; i < 64; i++)
                if ((pieces & (1UL << i)) != 0)
                    positions.Add(i);
            return positions;
        }

        public ulong GetHash()
        {
            ulong hash = 5648423;
            if (m_IsWhiteToMove)
                hash = 4239784;
            return m_BlackPieces ^ hash;
        }
        
        public ulong GetAllPieces()
        {
            return (m_BlackPieces | m_WhitePieces);
        }
        
        public int GetWinner()
        {
            var blackPieceCount = GetPieceCount(Piece.BLACK);
            var whitePieceCount = GetPieceCount(Piece.WHITE);
            if (blackPieceCount == whitePieceCount) return 0;
            return blackPieceCount > whitePieceCount ? Piece.BLACK : Piece.WHITE;
        }

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

        private int GetPieceColor(int index)
        {
            if (IsEmpty(index)) 
                return 0;
            return (m_BlackPieces & (1UL << index)) == 0 ? Piece.WHITE : Piece.BLACK;
        }

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
        
        private ulong IndexToBit(int id) 
        {
            ulong mask = 0x8000000000000000;
            int x = id >> 3;
            int y = id & 7;
            mask >>= y;
            mask >>= (x * 8);
            return mask;
        }
        
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