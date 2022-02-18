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
        
        public static HashSet<Move> GenerateLegalMoves(Board board)
        {
            var legalMoves = new HashSet<Move>();
            var emptySquares = board.GetEmptySquares();
            foreach (var square in emptySquares)
                for (var directionOffsetIndex = 0; directionOffsetIndex < 8; directionOffsetIndex++)
                    GenerateLegalMovesForSquare(board, square, directionOffsetIndex, legalMoves);
            return legalMoves;
        }

        private static void GenerateLegalMovesForSquare(Board board, int square, int directionOffsetIndex, HashSet<Move> legalMoves)
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
                legalMoves.Add(new Move(square, board.GetCurrentColorToMove()));
        }

        public static HashSet<int> GetCaptureIndices(Move move, Board board)
        {
            var captureIndices = new HashSet<int>();
            for (var directionOffsetIndex = 0; directionOffsetIndex < 8; directionOffsetIndex++)
                GenerateCapturesForSquare(move, board, directionOffsetIndex, captureIndices);
            return captureIndices;
        }

        private static void GenerateCapturesForSquare(Move move, Board board, int directionOffsetIndex, HashSet<int> captureIndices)
        {
            var captures = new HashSet<int>();
            var currentSquare = move.targetSquare + DirectionOffsets[directionOffsetIndex];
            if (Board.IsOutOfBounds(currentSquare)) return;

            for (var timesMoved = 1; timesMoved < SquaresToEdge[move.targetSquare][directionOffsetIndex]; timesMoved++)
            {
                if (!board.IsOpponentPiece(currentSquare)) break;
                captures.Add(currentSquare);
                currentSquare += DirectionOffsets[directionOffsetIndex];
            }
            
            if (board.IsFriendlyPiece(currentSquare) && captures.Count > 0)
                captureIndices.UnionWith(captures);
        }
    }
}