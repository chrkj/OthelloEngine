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
        
        public static Dictionary<int, HashSet<int>> GenerateLegalMoves(Board board)
        {
            var legalMoves = new Dictionary<int, HashSet<int>>();
            foreach (var square in board.GetEmptySquares())
                GenerateLegalMovesForSquare(board, square, legalMoves);
            return legalMoves;
        }

        private static void GenerateLegalMovesForSquare(Board board, int square, IDictionary<int, HashSet<int>> legalMoves)
        {
            var captures = new HashSet<int>();
            for (var directionOffsetIndex = 0; directionOffsetIndex < 8; directionOffsetIndex++)
            {
                var capturesCurrentDirection = new HashSet<int>();
                var currentSquare = square + DirectionOffsets[directionOffsetIndex];
                if (Board.IsOutOfBounds(currentSquare)) continue;

                for (var timesMoved = 1; timesMoved < SquaresToEdge[square][directionOffsetIndex]; timesMoved++)
                {
                    if (!board.IsOpponentPiece(currentSquare)) break;
                    capturesCurrentDirection.Add(currentSquare);
                    currentSquare += DirectionOffsets[directionOffsetIndex];
                }

                if (board.IsFriendlyPiece(currentSquare) && capturesCurrentDirection.Count > 0)
                    captures.UnionWith(capturesCurrentDirection);
            }
            if (captures.Count > 0)
                legalMoves.Add(square, captures);
        }
        
    }
}