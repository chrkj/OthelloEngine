using System;
using System.Collections.Generic;

namespace Othello.Core
{
    public static class MoveGenerator
    {
        private static readonly int[][] SquaresToEdge = new int[64][];
        private static readonly int[] DirectionOffsets = { 8, -8, -1, 1, 7, -7, 9, -9 };

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
                    
                    var squareIndex = Board.GetBoardIndex(file, rank);
                    SquaresToEdge[squareIndex] = new [] { numSquaresUp, numSquaresDown, numSquaresLeft, numSquaresRight, numSquaresUpLeft, numSquaresDownRight, numSquaresUpRight, numSquaresDownLeft };
                }
        }
        
        public static List<int> GenerateLegalMoves(Board board)
        {
            var legalMoves = new List<int>();
            var emptySquares = board.GetEmptySquares();
            foreach (var square in emptySquares)
                for (var directionOffsetIndex = 0; directionOffsetIndex < 8; directionOffsetIndex++)
                    GenerateLegalMovesForSquare(board, square, directionOffsetIndex, legalMoves);
            return legalMoves;
        }

        private static void GenerateLegalMovesForSquare(Board board, int square, int directionOffsetIndex, List<int>legalMoves)
        {
            var captureCount = 0;
            var currentSquare = square + DirectionOffsets[directionOffsetIndex];
            if (Board.IsOutOfBounds(currentSquare)) return;

            for (var timesMoved = 1; timesMoved < SquaresToEdge[square][directionOffsetIndex]; timesMoved++)
            {
                if (!board.IsOpponentPiece(currentSquare)) break;
                currentSquare += DirectionOffsets[directionOffsetIndex];
                captureCount++;
            }

            if (board.IsFriendlyPiece(currentSquare) && captureCount > 0)
                legalMoves.Add(square);
        }

        public static HashSet<int> GetCaptureIndices(int move, Board board)
        {
            var allCaptures = new HashSet<int>();
            for (var directionOffsetIndex = 0; directionOffsetIndex < 8; directionOffsetIndex++)
            {
                var currentCaptures = new HashSet<int>();
                var currentSquare = move + DirectionOffsets[directionOffsetIndex];
                if (Board.IsOutOfBounds(currentSquare)) continue;

                for (var timesMoved = 1; timesMoved < SquaresToEdge[move][directionOffsetIndex]; timesMoved++)
                {
                    if (!board.IsOpponentPiece(currentSquare)) break;
                    currentCaptures.Add(currentSquare);
                    currentSquare += DirectionOffsets[directionOffsetIndex];
                }

                if (board.IsFriendlyPiece(currentSquare) && currentCaptures.Count > 0)
                    allCaptures.UnionWith(currentCaptures);
            }
            return allCaptures;
        }

     
    }
}