using System;
using System.Collections.Generic;

namespace Othello.Core
{
    public static class MoveGenerator
    {
        private static readonly int[][] m_SquaresToEdge = new int[64][];
        private static readonly int[] m_DirectionOffsets = { 8, -8, -1, 1, 7, -7, 9, -9 };

        public static void PrecomputeData()
        {
            for (var file = 0; file < 8; file++)
                for (var rank = 0; rank < 8; rank++)
                {
                    var numSquaresUp = 7 - rank;
                    var numSquaresDown = rank;
                    var numSquaresLeft = file;
                    var numSquaresRight = 7 - file;
                    var numSquaresUpLeft = Math.Min(numSquaresUp, numSquaresLeft);
                    var numSquaresDownRight = Math.Min(numSquaresDown, numSquaresRight);
                    var numSquaresUpRight = Math.Min(numSquaresUp, numSquaresRight);
                    var numSquaresDownLeft = Math.Min(numSquaresDown, numSquaresLeft);
                    
                    var squareIndex = Board.GetIndex(file, rank);
                    m_SquaresToEdge[squareIndex] = new [] { numSquaresUp, numSquaresDown, numSquaresLeft, numSquaresRight, numSquaresUpLeft, numSquaresDownRight, numSquaresUpRight, numSquaresDownLeft };
                }
        }
        
        public static List<Move> GenerateLegalMoves(Board board)
        {
            var legalMoves = new List<Move>();
            var emptySquares = board.GetEmptySquares();
            foreach (var square in emptySquares)
                GenerateLegalMovesForSquare(board, square, legalMoves);
            return legalMoves;
        }

        private static void GenerateLegalMovesForSquare(Board board, int square, ICollection<Move> legalMoves)
        {
            for (var directionOffsetIndex = 0; directionOffsetIndex < 8; directionOffsetIndex++)
            {
                var captureCount = 0;
                var currentSquare = square + m_DirectionOffsets[directionOffsetIndex];
                if (Board.IsOutOfBounds(currentSquare)) continue;

                for (var timesMoved = 1; timesMoved < m_SquaresToEdge[square][directionOffsetIndex]; timesMoved++)
                {
                    if (!board.IsOpponentPiece(currentSquare)) break;
                    captureCount++;
                    currentSquare += m_DirectionOffsets[directionOffsetIndex];
                }

                if (!board.IsFriendlyPiece(currentSquare) || captureCount <= 0) continue;
                legalMoves.Add(new Move(square));
                break;
            }
        }

        public static HashSet<Move> GetCaptureIndices(Move move, Board board)
        {
            var allCaptures = new HashSet<Move>();
            for (var directionOffsetIndex = 0; directionOffsetIndex < 8; directionOffsetIndex++)
            {
                var currentCaptures = new HashSet<Move>();
                var currentSquare = move.Index + m_DirectionOffsets[directionOffsetIndex];
                if (Board.IsOutOfBounds(currentSquare)) continue;

                for (var timesMoved = 1; timesMoved < m_SquaresToEdge[move.Index][directionOffsetIndex]; timesMoved++)
                {
                    if (!board.IsOpponentPiece(currentSquare)) break;
                    currentCaptures.Add(new Move(currentSquare));
                    currentSquare += m_DirectionOffsets[directionOffsetIndex];
                }

                if (!board.IsFriendlyPiece(currentSquare) || currentCaptures.Count <= 0) continue;
                allCaptures.UnionWith(currentCaptures);
            }
            return allCaptures;
        }

     
    }
}