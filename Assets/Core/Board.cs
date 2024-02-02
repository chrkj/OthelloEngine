using System;
using System.Collections.Generic;

namespace Othello.Core
{
    public class Board
    {
        private Move m_LastMove;
        private ulong m_BlackPieces;
        private ulong m_WhitePieces;
        private bool m_IsWhiteToMove;
        private const int MAX_LEGAL_MOVES = 30;

        public Board()
        {
            m_LastMove = null;
            m_IsWhiteToMove = false;
        }

        public Board Copy()
        {
            var copy = new Board
            {
                m_BlackPieces = m_BlackPieces,
                m_LastMove = m_LastMove,
                m_IsWhiteToMove = m_IsWhiteToMove,
                m_WhitePieces = m_WhitePieces
            };
            return copy;
        }

        public void ResetBoard(int playerToStart)
        {
            m_BlackPieces = 0;
            m_WhitePieces = 0;
            m_LastMove = null;
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

            var numLegalMovesCurrentPlayer = GenerateLegalMoves().Length;
            if (numLegalMovesCurrentPlayer != 0)
                return false;

            ChangePlayer();
            var numLegalMovesCurrentOpponent = GenerateLegalMoves().Length;
            ChangePlayer();

            return numLegalMovesCurrentOpponent == 0;
        }

        public void MakeMove(Move move)
        {
            m_LastMove = move;
            PlacePiece(move.Index, GetCurrentPlayer());
            var captures = GetCaptureIndices(move);
            foreach (var capture in captures)
                PlacePiece(capture.Index, GetCurrentPlayer());
        }

        public bool Equals(Board other)
        {
            return m_WhitePieces == other.m_WhitePieces && m_BlackPieces == other.m_BlackPieces && m_IsWhiteToMove == other.m_IsWhiteToMove;
        }

        public Move[] GenerateLegalMoves()
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
            
            ulong horizontalEdgeBitmap = opponentBitBoard & 0x7e7e7e7e7e7e7e7e;
            ulong verticalEdgeBitmap = opponentBitBoard & 0x00FFFFFFFFFFFF00;
            ulong allEdgeBitmap = opponentBitBoard & 0x007e7e7e7e7e7e00;
            ulong blankBoard = ~(m_BlackPieces | m_WhitePieces);
            
            ulong tmp;
            ulong legalMovesBitBoard;
            tmp = horizontalEdgeBitmap & (currentPlayerBitboard << 1);
            for (int i = 0; i < 5; ++i)
                tmp |= horizontalEdgeBitmap & (tmp << 1);
            legalMovesBitBoard = blankBoard & (tmp << 1);

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

            return BitBoardToList(legalMovesBitBoard);
        }
        
        private Move[] BitBoardToList(ulong bitboard)
        {
            int currIndexPtr = 0;
            Move[] setBitIndices = new Move[MAX_LEGAL_MOVES];
            for (int i = 0; i < sizeof(ulong) * 8; i++)
            {
                ulong mask = (ulong)1 << i;
                if ((bitboard & mask) != 0)
                    setBitIndices[currIndexPtr++] = new Move(i);
            }
            var returnArray = new Move[currIndexPtr];
            Array.Copy(setBitIndices, returnArray, currIndexPtr);
            return returnArray;
        }

        public void LoadStartPosition()
        {
            PlacePiece(27, Piece.BLACK);
            PlacePiece(28, Piece.WHITE);
            PlacePiece(35, Piece.WHITE);
            PlacePiece(36, Piece.BLACK);
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

        public Move GetLastMove()
        {
            return m_LastMove;
        }

        public static bool IsOutOfBounds(int file, int rank)
        {
            return file < 0 | file > 7 | rank < 0 | rank > 7;
        }

        public int GetCurrentPlayer()
        {
            return m_IsWhiteToMove ? Piece.WHITE : Piece.BLACK;
        }

        public void ChangePlayer()
        {
            m_IsWhiteToMove = !m_IsWhiteToMove;
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

        private int GetCurrentOpponent()
        {
            return m_IsWhiteToMove ? Piece.BLACK : Piece.WHITE;
        }

        private static bool IsOutOfBounds(int index)
        {
            return index < 0 || index > 63;
        }

        private bool IsOpponentPiece(int index)
        {
            return GetPieceColor(index) == GetCurrentOpponent();
        }

        private bool IsFriendlyPiece(int index)
        {
            return GetPieceColor(index) == GetCurrentPlayer();
        }

        public int GetWinner()
        {
            var blackPieceCount = GetPieceCount(Piece.BLACK);
            var whitePieceCount = GetPieceCount(Piece.WHITE);
            if (blackPieceCount == whitePieceCount) return 0;
            return blackPieceCount > whitePieceCount ? Piece.BLACK : Piece.WHITE;
        }

        private HashSet<Move> GetCaptureIndices(Move move)
        {
            var allCaptures = new HashSet<Move>();
            for (var directionOffsetIndex = 0; directionOffsetIndex < 8; directionOffsetIndex++)
            {
                var currentCaptures = new HashSet<Move>();
                var currentSquare = move.Index + MoveData.s_DirectionOffsets[directionOffsetIndex];
                if (IsOutOfBounds(currentSquare)) continue;

                for (var timesMoved = 1;
                     timesMoved < MoveData.s_SquaresToEdge[move.Index][directionOffsetIndex];
                     timesMoved++)
                {
                    if (!IsOpponentPiece(currentSquare)) break;
                    currentCaptures.Add(new Move(currentSquare));
                    currentSquare += MoveData.s_DirectionOffsets[directionOffsetIndex];
                }

                if (!IsFriendlyPiece(currentSquare) || currentCaptures.Count <= 0)
                    continue;

                allCaptures.UnionWith(currentCaptures);
            }

            return allCaptures;
        }

        public List<int> GetPiecePositionsBlack()
        {
            var positions = new List<int>();
            var blackPositions = m_BlackPieces;
            for (int i = 0; i < 64; i++)
                if ((blackPositions & (1UL << i)) != 0)
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
    }
}