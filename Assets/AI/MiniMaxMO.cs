using System;
using Othello.Core;
using Console = Othello.Core.Console;

namespace Othello.AI
{
    public class MiniMaxMO : ISearchEngine
    {
        private static int m_positions;
        private readonly int m_depthLimit;
        private const int m_ParityWeight = 1;
        private const int m_CornerWeight = 4;
        private const int m_MaxPlayer = Piece.Black;
        private const int m_MinPlayer = Piece.White;

        public MiniMaxMO(int depth)
        {
            m_depthLimit = depth;
        }
        
        public Move StartSearch(Board board)
        {
            return CalculateMove(board);
        }
        
        private Move CalculateMove(Board board)
        {
            m_positions = 0;
            Move bestMove = null;
            var currentPlayer = board.GetCurrentPlayer();
            var start = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            int currentUtil;
            if (currentPlayer == m_MaxPlayer)
            {
                var highestUtil = int.MinValue;
                var legalMoves = board.GenerateLegalMoves();
                legalMoves.Sort();
                foreach (var legalMove in legalMoves)
                {
                    var possibleNextState = MakeMove(board, legalMove);
                    currentUtil = MinValue(possibleNextState, m_depthLimit - 1, int.MinValue, int.MaxValue);
                    if (currentUtil <= highestUtil) 
                        continue;
                    highestUtil = currentUtil;
                    bestMove = legalMove;
                }
            }
            else
            {
                var minUtil = int.MaxValue;
                var legalMoves = board.GenerateLegalMoves();
                legalMoves.Sort();
                foreach (var legalMove in legalMoves)
                {
                    var possibleNextState = MakeMove(board, legalMove);
                    currentUtil = MaxValue(possibleNextState, m_depthLimit - 1, int.MinValue, int.MaxValue);
                    if (currentUtil >= minUtil) 
                        continue;
                    minUtil = currentUtil;
                    bestMove = legalMove;
                }
            }
            var end = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            Console.Log(board.GetCurrentPlayerAsString() + " plays " + bestMove.ToString());
            Console.Log("Search time: " + (end - start) + " ms");
            Console.Log("Positions examined: " + m_positions);
            Console.Log("----------------------------------------------------");
            return bestMove;
        }

        private int MinValue(Board board, int depth, int alpha, int beta)
        {
            if (IsTerminal(board, depth))
                return GetUtility(board);
            
            var minUtil = int.MaxValue - 1;

            var legalMoves = board.GenerateLegalMoves();
            if (legalMoves.Count == 0)
                minUtil = Math.Min(minUtil, MaxValue(board, depth - 1, alpha, beta));
            
            legalMoves.Sort();
            foreach (var legalMove in legalMoves)
            {
                var nextState = MakeMove(board, legalMove);
                minUtil = Math.Min(minUtil, MaxValue(nextState, depth - 1, alpha, beta));
                if (minUtil <= alpha) 
                    return minUtil;
                beta = Math.Min(beta, minUtil);
            }
            return minUtil;
        }

        private int MaxValue(Board board, int depth, int alpha, int beta)
        {
            if (IsTerminal(board, depth))
                return GetUtility(board);

            var maxUtil = int.MinValue + 1;

            var legalMoves = board.GenerateLegalMoves();
            if (legalMoves.Count == 0)
                maxUtil = Math.Max(maxUtil, MinValue(board, depth - 1, alpha, beta));
            
            legalMoves.Sort();
            foreach (var legalMove in legalMoves)
            {
                var nextState = MakeMove(board, legalMove);
                maxUtil = Math.Max(maxUtil, MinValue(nextState, depth - 1, alpha, beta));
                if (maxUtil >= beta) 
                    return maxUtil;
                alpha = Math.Max(alpha, maxUtil);
            }
            return maxUtil;
        }

        private static int GetUtility(Board board)
        {
            m_positions++;
            if (!board.IsTerminalBoardState())
                return GetBoardUtility(board);
            if (board.GetPieceCount(m_MaxPlayer) > board.GetPieceCount(m_MinPlayer)) 
                return int.MaxValue - 1;
            if (board.GetPieceCount(m_MaxPlayer) < board.GetPieceCount(m_MinPlayer)) 
                return int.MinValue + 1;
            return 0;
        }

        private static bool IsTerminal(Board board, int depth)
        {
            return board.IsTerminalBoardState() || depth == 0;
        }
        
        private static Board MakeMove(Board board, Move legalMove)
        {
            var nextBoardState = board.Copy();
            nextBoardState.MakeMove(legalMove);
            nextBoardState.ChangePlayer();
            return nextBoardState;
        }
        
        private static int GetBoardUtility(Board board)
        {
            int value = 0;
            var positions = board.GetPiecePositionsBlack();
            foreach (var pos in positions)
                value += Move.m_cellWeight[pos];
            return m_ParityWeight * TokenParityValue(board) + m_CornerWeight * TokenCornerValue(board) + value;
        }
        
        private static int TokenParityValue(Board board)
        {
            return board.GetPieceCount(m_MaxPlayer);
        }
        
        private static int TokenCornerValue(Board board)
        {
            var maxPlayerCornerValue = 0;
            var minPlayerCornerValue = 0;

            if (board.GetPieceColor(0, 0) == m_MaxPlayer)
                maxPlayerCornerValue++;
            else if (board.GetPieceColor(0, 0) == m_MinPlayer)
                minPlayerCornerValue++;

            if (board.GetPieceColor(0, 7) == m_MaxPlayer)
                maxPlayerCornerValue++;
            else if (board.GetPieceColor(0, 7) == m_MinPlayer)
                minPlayerCornerValue++;

            if (board.GetPieceColor(7, 0) == m_MaxPlayer)
                maxPlayerCornerValue++;
            else if (board.GetPieceColor(7, 0) == m_MinPlayer)
                minPlayerCornerValue++;

            if (board.GetPieceColor(7, 7) == m_MaxPlayer)
                maxPlayerCornerValue++;
            else if (board.GetPieceColor(7, 7) == m_MinPlayer)
                minPlayerCornerValue++;
            
            return maxPlayerCornerValue - minPlayerCornerValue;
        }
    }
}