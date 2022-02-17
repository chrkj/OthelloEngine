using System;
using System.Collections.Generic;

namespace Othello.Core
{
    public class MoveGenerator
    {
        private static readonly int[][] NumSquaresToEdge = new int[64][];
        private static readonly int[] AdjacentSquareOffsets = new[] { 8, -8, -1, 1, 7, -7, 9, -9 };

        public MoveGenerator()
        {
            PrecomputeMoveDate();
        }
        
        private void PrecomputeMoveDate()
        {
            for (var file = 0; file < 8; file++)
            {
                for (var rank = 0; rank < 8; rank++)
                {
                    var numSquaresUp = 7 - rank;
                    var numSquaresDown = rank;
                    var numSquaresLeft = file;
                    var numSquaresLeftRight = 7 - file;

                    var numSquaresUpLeft = Math.Min(numSquaresUp, numSquaresLeft);
                    var numSquaresDownRight = Math.Min(numSquaresDown, numSquaresLeftRight);
                    var numSquaresUpRight = Math.Min(numSquaresUp, numSquaresLeftRight);
                    var numSquaresDownLeft = Math.Min(numSquaresDown, numSquaresLeft);
                    
                    var squareIndex = Board.GetBoardIndex(file, rank);
                    NumSquaresToEdge[squareIndex] = new []
                        { 
                            numSquaresUp, 
                            numSquaresDown, 
                            numSquaresLeft, 
                            numSquaresLeftRight, 
                            numSquaresUpLeft,
                            numSquaresDownRight,
                            numSquaresUpRight,
                            numSquaresDownLeft
                        };
                }
            }
        }
        
        public HashSet<Move> GenerateLegalMoves(Board board)
        {
            var legalMoves = new HashSet<Move>();
            var emptySquares = board.GetEmptySquares();
            foreach (var square in emptySquares)
                for (var squareOffsetIndex = 0; squareOffsetIndex < 8; squareOffsetIndex++)
                    GenerateLegalMovesForSquare(board, square, squareOffsetIndex, legalMoves);
            return legalMoves;
        }

        private static void GenerateLegalMovesForSquare(Board board, int square, int squareOffsetIndex, HashSet<Move> legalMoves)
        {
            var captureCount = 0;
            var currentSquare = square + AdjacentSquareOffsets[squareOffsetIndex];
            if (Board.IsOutOfBounds(currentSquare)) return;

            var timesMoved = 0;
            while (board.IsOpponentPiece(currentSquare) && timesMoved < NumSquaresToEdge[square][squareOffsetIndex])
            {
                timesMoved++;
                captureCount++;
                currentSquare += AdjacentSquareOffsets[squareOffsetIndex];
                if (Board.IsOutOfBounds(currentSquare)) break;
            }

            if (Board.IsOutOfBounds(currentSquare)) return;
            if (board.IsFriendlyPiece(currentSquare) && captureCount > 0)
                legalMoves.Add(new Move(square, board.GetCurrentColorToMove()));
        }

        public static HashSet<int> GetCaptureIndices(Move move, Board board)
        {
            var captureIndices = new HashSet<int>();
            for (var squareOffsetIndex = 0; squareOffsetIndex < 8; squareOffsetIndex++)
                GenerateCapturesForSquare(move, board, squareOffsetIndex, captureIndices);
            return captureIndices;
        }

        private static void GenerateCapturesForSquare(Move move, Board board, int squareOffsetIndex, HashSet<int> captureIndices)
        {
            var captures = new HashSet<int>();
            var currentIndex = move.TargetSquare + AdjacentSquareOffsets[squareOffsetIndex];
            if (Board.IsOutOfBounds(currentIndex)) return;

            var timesMoved = 0;
            while (board.IsOpponentPiece(currentIndex) && timesMoved < NumSquaresToEdge[move.TargetSquare][squareOffsetIndex])
            {
                timesMoved++;
                captures.Add(currentIndex);
                currentIndex += AdjacentSquareOffsets[squareOffsetIndex];
                if (Board.IsOutOfBounds(currentIndex)) break;
            }

            if (Board.IsOutOfBounds(currentIndex)) return;
            if (board.IsFriendlyPiece(currentIndex) && captures.Count > 0)
                captureIndices.UnionWith(captures);
        }
    }
}