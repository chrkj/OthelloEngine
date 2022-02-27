using System;

namespace Othello.Core
{
    public static class MoveData
    {
        public static readonly int[][] SquaresToEdge = new int[64][];
        public static readonly int[] DirectionOffsets = { 8, -8, -1, 1, 7, -7, 9, -9 };

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
                    SquaresToEdge[squareIndex] = new [] { numSquaresUp, numSquaresDown, numSquaresLeft, numSquaresRight, numSquaresUpLeft, numSquaresDownRight, numSquaresUpRight, numSquaresDownLeft };
                }
        }

    }
}