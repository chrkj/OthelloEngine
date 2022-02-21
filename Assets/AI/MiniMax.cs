using System;
using System.Collections.Generic;
using UnityEngine;
using Othello.Core;

namespace Othello.AI
{
    public class MiniMax : ISearchEngine
    {
        private readonly int _depthLimit;
        
        private const int ParityWeight = 1;
        private const int CornerWeight = 4;
        
        private const int MaxPlayer = Piece.Black;
        private const int MinPlayer = Piece.White;

        private static int positions;

        public MiniMax(int depth)
        {
            _depthLimit = depth;
        }
        
        public Move StartSearch(Board board)
        {
            return CalculateMove(board);
        }
        
        private Move CalculateMove(Board board)
        {
            positions = 0;
            var currentUtil = 0;
            Move bestMove = null;
            var currentPlayer = board.GetCurrentPlayer();

            if (currentPlayer == MaxPlayer)
            {
                var highestUtil = int.MinValue;
                foreach (var legalMove in MoveGenerator.GenerateLegalMoves(board)) 
                {
                    var possibleNextState = MakeMove(board, legalMove);
                    currentUtil = MinValue(possibleNextState, _depthLimit - 1, int.MinValue, int.MaxValue);
                    if (currentUtil <= highestUtil) continue;
                    highestUtil = currentUtil;
                    bestMove = new Move(legalMove.Key, currentPlayer, legalMove.Value);
                }
            }
            else
            {
                var start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                var minUtil = int.MaxValue;
                foreach (var legalMove in MoveGenerator.GenerateLegalMoves(board)) 
                {
                    var possibleNextState = MakeMove(board, legalMove);
                    currentUtil = MaxValue(possibleNextState, _depthLimit - 1, int.MinValue, int.MaxValue);
                    if (currentUtil >= minUtil) continue;
                    minUtil = currentUtil;
                    bestMove = new Move(legalMove.Key, currentPlayer, legalMove.Value);;
                }
                var end = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                MonoBehaviour.print("Time taken: " + (end - start) + "ms for " + positions + " positions");
            }
            return bestMove;
        }

        private int MinValue(Board board, int depth, int alpha, int beta)
        {
            if (IsTerminal(board, depth))
                return GetUtility(board);
            
            var minUtil = int.MaxValue - 1;
            if (!HasLegalMove(board))
                minUtil = Math.Min(minUtil, MaxValue(board, depth - 1, alpha, beta));
            
            foreach (var legalMove in MoveGenerator.GenerateLegalMoves(board))
            {
                var nextState = MakeMove(board, legalMove);
                minUtil = Math.Min(minUtil, MaxValue(nextState, depth - 1, alpha, beta));
                if (minUtil <= alpha) return minUtil;
                beta = Math.Min(beta, minUtil);
            }
            return minUtil;
        }

        private int MaxValue(Board board, int depth, int alpha, int beta)
        {
            if (IsTerminal(board, depth))
                return GetUtility(board);

            var maxUtil = int.MinValue + 1;
            if (!HasLegalMove(board))
                maxUtil = Math.Max(maxUtil, MinValue(board, depth - 1, alpha, beta));
            
            foreach (var legalMove in MoveGenerator.GenerateLegalMoves(board))
            {
                var nextState = MakeMove(board, legalMove);
                maxUtil = Math.Max(maxUtil, MinValue(nextState, depth - 1, alpha, beta));
                if (maxUtil >= beta) return maxUtil;
                alpha = Math.Max(alpha, maxUtil);
            }
            return maxUtil;
        }
        
        private static bool HasLegalMove(Board board)
        {
            return MoveGenerator.GenerateLegalMoves(board).Count != 0;
        }

        private static int GetUtility(Board board)
        {
            positions++;
            if (!board.IsTerminalBoardState(board)) return GetBoardUtility(board);
            if (board.GetPieceCount(MaxPlayer) > board.GetPieceCount(MinPlayer)) return int.MaxValue - 1;
            if (board.GetPieceCount(MaxPlayer) < board.GetPieceCount(MinPlayer)) return int.MinValue + 1;
            return 0;
        }

        private static bool IsTerminal(Board board, int depth)
        {
            return board.IsTerminalBoardState(board) || depth == 0;
        }
        
        private static Board MakeMove(Board board, KeyValuePair<int,HashSet<int>> legalMove)
        {
            var nextBoardState = new Board(board);
            nextBoardState.MakeMove(new Move(legalMove.Key, board.GetCurrentPlayer(), legalMove.Value));
            nextBoardState.ChangePlayer();
            return nextBoardState;
        }
        
        private static int GetBoardUtility(Board board)
        {
            return ParityWeight * TokenParityValue(board) + CornerWeight * TokenCornerValue(board);
        }
        
        private static int TokenParityValue(Board board)
        {
            return board.GetPieceCount(MaxPlayer);
        }
        
        private static int TokenCornerValue(Board board)
        {
            var maxPlayerCornerValue = 0;
            var minPlayerCornerValue = 0;

            if (board.GetPiece(0, 0) == MaxPlayer)
                maxPlayerCornerValue++;
            else if (board.GetPiece(0, 0) == MinPlayer)
                minPlayerCornerValue++;

            if (board.GetPiece(0, 7) == MaxPlayer)
                maxPlayerCornerValue++;
            else if (board.GetPiece(0, 7) == MinPlayer)
                minPlayerCornerValue++;

            if (board.GetPiece(7, 0) == MaxPlayer)
                maxPlayerCornerValue++;
            else if (board.GetPiece(7, 0) == MinPlayer)
                minPlayerCornerValue++;

            if (board.GetPiece(7, 7) == MaxPlayer)
                maxPlayerCornerValue++;
            else if (board.GetPiece(7, 7) == MinPlayer)
                minPlayerCornerValue++;
            
            return maxPlayerCornerValue - minPlayerCornerValue;
        }
    }
}