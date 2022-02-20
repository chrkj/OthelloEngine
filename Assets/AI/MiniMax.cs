using System;
using Othello.Core;

namespace Othello.AI
{
    public class MiniMax : ISearchEngine
    {
        private const int DepthLimit = 6;
        
        public Move StartSearch(Board board)
        {
            return DecideMove(board);
        }

        private Move DecideMove(Board board)
        {
            Move bestMove = null;
            int currentUtil;
            var currentPlayer = board.GetCurrentColorToMove();

            if (currentPlayer == Piece.Black)
            {
                var highestUtil = int.MinValue;
                foreach (var legalMove in MoveGenerator.GenerateLegalMoves(board))
                {
                    var possibleNextState = new Board(board);
                    possibleNextState.MakeMove(new Move(legalMove.Key, board.GetColorToMove(), legalMove.Value));
                    currentUtil = MinValue(possibleNextState, DepthLimit - 1, int.MinValue, int.MaxValue);

                    if (currentUtil <= highestUtil) continue;
                    highestUtil = currentUtil;
                    bestMove = new Move(legalMove.Key, board.GetColorToMove(), legalMove.Value);
                }
            }
            else
            {
                var minUtil = int.MaxValue;
                foreach (var legalMove in MoveGenerator.GenerateLegalMoves(board))
                {
                    var possibleNextState = new Board(board);
                    possibleNextState.MakeMove(new Move(legalMove.Key, board.GetColorToMove(), legalMove.Value));
                    currentUtil = MaxValue(possibleNextState, DepthLimit - 1, int.MinValue, int.MaxValue);

                    if (currentUtil >= minUtil) continue;
                    minUtil = currentUtil;
                    bestMove = new Move(legalMove.Key, board.GetColorToMove(), legalMove.Value);
                }
            }
            return bestMove;
        }
        
        private static int Utility(Board board)
        {
            return board.GetPieceCount(Piece.Black);
        }
        
        private static bool TerminalTest(Board board)
        {
            return board.IsTerminalState(board);
        }

        private int MaxValue(Board board, int depth, int alpha, int beta)
        {
            if (TerminalTest(board) || depth == 0) return Utility(board);
            var v = int.MinValue;

            if (MoveGenerator.GenerateLegalMoves(board).Count == 0) v = Math.Max(v, MinValue(board, depth - 1, alpha, beta));

            foreach (var legalMove in MoveGenerator.GenerateLegalMoves(board)) 
            {
                var possibleNextState = new Board(board);
                possibleNextState.MakeMove(new Move(legalMove.Key, board.GetColorToMove(), legalMove.Value));
                v = Math.Max(v, MinValue(possibleNextState, depth - 1, alpha, beta));

                if (v >= beta) return v;
                alpha = Math.Max(alpha, v);
            }
            return v;
        }

        private int MinValue(Board board, int depth, int alpha, int beta)
        {
            if (TerminalTest(board) || depth == 0) return Utility(board);
            var v = int.MaxValue;

            if (MoveGenerator.GenerateLegalMoves(board).Count == 0) v = Math.Min(v, MaxValue(board, depth - 1, alpha, beta));

            foreach (var legalMove in MoveGenerator.GenerateLegalMoves(board)) 
            {
                var possibleNextState = new Board(board);
                possibleNextState.MakeMove(new Move(legalMove.Key, board.GetColorToMove(), legalMove.Value));
                v = Math.Min(v, MaxValue(possibleNextState, depth - 1, alpha, beta));

                if (v <= alpha) return v;
                beta = Math.Min(beta, v);
            }
            return v;
        }
    }
}