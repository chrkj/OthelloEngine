using System;
using System.Collections.Generic;

using Othello.Core;
using Console = Othello.Core.Console;

namespace Othello.AI
{
    public class MiniMax : ISearchEngine
    {
        public static int s_WhiteCurrentDepth;
        public static int s_BlackCurrentDepth;
        public static int s_WhiteZobristSize;
        public static int s_BlackZobristSize;
        public static int s_WhiteBranchesPruned;
        public static int s_BlackBranchesPruned;
        public static int s_WhitePositionsEvaluated;
        public static int s_BlackPositionsEvaluated;
        
        private const int MAX_PLAYER = Piece.BLACK;
        private const int MIN_PLAYER = Piece.WHITE;
        
        private long m_TimeLimit;
        private int m_CurrentPlayer;
        private bool m_TerminationFlag;
        private readonly int m_MaxTime;
        private readonly int m_DepthLimit;
        private readonly bool m_MoveOrderingEnabled;
        private readonly bool m_ZobristHashingEnabled;
        private readonly bool m_IterativeDeepeningEnabled;
        private readonly Dictionary<ulong, int> m_Zobrist;

        public MiniMax(int depth, int timeLimit, bool moveOrderingEnabled, bool iterativeDeepeningEnabled, bool zobristHashingEnabled)
        {
            m_IterativeDeepeningEnabled = iterativeDeepeningEnabled;
            m_MoveOrderingEnabled = moveOrderingEnabled;
            m_ZobristHashingEnabled = zobristHashingEnabled;
            m_DepthLimit = depth;
            m_MaxTime = timeLimit;
            s_WhiteZobristSize = 0;
            s_BlackZobristSize = 0;
            m_Zobrist = new Dictionary<ulong, int>();
        }

        public Move StartSearch(Board board)
        {
            m_TerminationFlag = false;
            m_CurrentPlayer = board.GetCurrentPlayer() == Piece.BLACK ? Piece.BLACK : Piece.WHITE;
            ResetBranchCount();

            var start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            m_TimeLimit = start + m_MaxTime;

            int bestEvalThisIteration = 0;
            Span<Move> legalMoves = stackalloc Move[256];
            board.GenerateLegalMovesStack(ref legalMoves);
            Move bestMoveThisIteration = legalMoves[0];
            
            if (m_IterativeDeepeningEnabled)
            {
                for (int searchDepth = 1; searchDepth < m_DepthLimit + 1; searchDepth++)
                {
                    ResetBranchCount();
                    UpdateSearchDepth(board, searchDepth);
                    MoveOrdering(bestMoveThisIteration, ref legalMoves);
                    CalculateMove(board, ref bestMoveThisIteration, ref bestEvalThisIteration, searchDepth);
                    if (m_TerminationFlag)
                        break;
                }
            }
            else
            {
                CalculateMove(board, ref bestMoveThisIteration, ref bestEvalThisIteration, m_DepthLimit);
            }
            PrintSearchData(board, start, bestMoveThisIteration);
            return bestMoveThisIteration;
        }

        private void CalculateMove(Board board, ref Move bestMoveThisIteration, ref int bestEvalThisIteration, int depth)
        {
            ResetPositionCount();
            var currentPlayer = board.GetCurrentPlayer();
            if (currentPlayer == MAX_PLAYER)
            {
                var maxEval = int.MinValue;
                Span<Move> legalMoves = stackalloc Move[256];
                board.GenerateLegalMovesStack(ref legalMoves);
                foreach (var legalMove in legalMoves)
                {
                    if (m_TerminationFlag)
                        break;
                    var possibleNextState = MakeMove(board, legalMove);
                    bestEvalThisIteration = MinValue(possibleNextState, depth - 1, int.MinValue, int.MaxValue);
                    if (bestEvalThisIteration <= maxEval)
                        continue;
                    maxEval = bestEvalThisIteration;
                    bestMoveThisIteration = legalMove;
                }
            }
            else
            {
                var minEval = int.MaxValue;
                Span<Move> legalMoves = stackalloc Move[256];
                board.GenerateLegalMovesStack(ref legalMoves);
                foreach (var legalMove in legalMoves)
                {
                    if (m_TerminationFlag)
                        break;
                    var possibleNextState = MakeMove(board, legalMove);
                    bestEvalThisIteration = MaxValue(possibleNextState, depth - 1, int.MinValue, int.MaxValue);
                    if (bestEvalThisIteration >= minEval)
                        continue;
                    minEval = bestEvalThisIteration;
                    bestMoveThisIteration = legalMove;
                }
            }
        }

        private int MinValue(Board board, int depth, int alpha, int beta)
        {
            CheckTimelimit();
            if (IsTerminal(board, depth))
                return GetEval(board);
            if (m_TerminationFlag)
                return int.MaxValue;

            var minUtil = int.MaxValue - 1;

            Span<Move> legalMoves = stackalloc Move[256];
            board.GenerateLegalMovesStack(ref legalMoves);
            if (legalMoves.Length == 0)
                minUtil = Math.Min(minUtil, MaxValue(board, depth - 1, alpha, beta));

            foreach (var legalMove in legalMoves)
            {
                var nextState = MakeMove(board, legalMove);
                minUtil = Math.Min(minUtil, MaxValue(nextState, depth - 1, alpha, beta));
                if (minUtil <= alpha)
                {
                    IncrementPruneCount();
                    return minUtil;
                }
                beta = Math.Min(beta, minUtil);
            }
            return minUtil;
        }

        private int MaxValue(Board board, int depth, int alpha, int beta)
        {
            CheckTimelimit();
            if (IsTerminal(board, depth))
                return GetEval(board);
            if (m_TerminationFlag)
                return int.MinValue;

            var maxUtil = int.MinValue + 1;

            Span<Move> legalMoves = stackalloc Move[256];
            board.GenerateLegalMovesStack(ref legalMoves);
            if (legalMoves.Length == 0)
                maxUtil = Math.Max(maxUtil, MinValue(board, depth - 1, alpha, beta));

            foreach (var legalMove in legalMoves)
            {
                var nextState = MakeMove(board, legalMove);
                maxUtil = Math.Max(maxUtil, MinValue(nextState, depth - 1, alpha, beta));
                if (maxUtil >= beta)
                {
                    IncrementPruneCount();
                    return maxUtil;
                }
                alpha = Math.Max(alpha, maxUtil);
            }
            return maxUtil;
        }

        private int GetEval(Board board)
        {
            IncrementPositionCount();

            var boardHash = board.GetHash();
            if (m_ZobristHashingEnabled && m_Zobrist.TryGetValue(boardHash, out var zobristEval))
                return zobristEval;
            
            if (!board.IsTerminalBoardState())
            {
                int eval = EvaluateBoard(board);
                if (!m_ZobristHashingEnabled) 
                    return eval;
                
                m_Zobrist[boardHash] = eval;
                IncrementZobristCount(1);
                return eval;
            }
            
            if (board.GetPieceCount(MAX_PLAYER) > board.GetPieceCount(MIN_PLAYER))
                return int.MaxValue - 1;
            if (board.GetPieceCount(MAX_PLAYER) < board.GetPieceCount(MIN_PLAYER))
                return int.MinValue + 1;
            return 0;
        }

        private void IncrementZobristCount(int count)
        {
            if (m_CurrentPlayer == Piece.BLACK)
                s_BlackZobristSize += count;
            else
                s_WhiteZobristSize += count;
        }

        private void IncrementPositionCount()
        {
            if (m_CurrentPlayer == Piece.BLACK)
                s_BlackPositionsEvaluated++;
            else
                s_WhitePositionsEvaluated++;
        }

        private static bool IsTerminal(Board board, int depth)
        {
            return board.IsTerminalBoardState() || depth == 0;
        }

        private static Board MakeMove(Board board, Move legalMove)
        {
            var nextBoardState = board.Copy();
            nextBoardState.MakeMove(legalMove);
            nextBoardState.ChangePlayerToMove();
            return nextBoardState;
        }

        private static int EvaluateBoard(Board board)
        {
            int value = 0;
            var positions = board.GetPiecePositionsBlack();
            foreach (var pos in positions)
                value += Move.s_CellWeight[pos];
            return value;
        }

        private void PrintSearchData(Board board, long start, Move bestMove)
        {
            var end = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            Console.Log(board.GetCurrentPlayerAsString() + " plays " + bestMove);
            Console.Log("Search time: " + (end - start) + " ms");
            Console.Log("Positions examined: " + (m_CurrentPlayer == Piece.BLACK ? s_BlackPositionsEvaluated : s_WhitePositionsEvaluated));
            Console.Log("----------------------------------------------------");
        }

        private void MoveOrdering(Move bestMove, ref Span<Move> legalMoves)
        {
            if (!m_MoveOrderingEnabled)
                return;
            if (bestMove == Move.NULLMOVE)
                return;

            // TODO: Sort the span for move ordering
            
            var targetIndex = legalMoves.IndexOf(bestMove);
            // Swap the bestMove to the front of the array
            (legalMoves[0], legalMoves[targetIndex]) = (legalMoves[targetIndex], legalMoves[0]);
        }

        private void IncrementPruneCount()
        {
            if (m_CurrentPlayer == Piece.BLACK)
                s_BlackBranchesPruned++;
            else
                s_WhiteBranchesPruned++;
        }

        private void UpdateSearchDepth(Board board, int searchDepth)
        {
            if (board.GetCurrentPlayer() == Piece.WHITE)
                s_WhiteCurrentDepth = searchDepth;
            else
                s_BlackCurrentDepth = searchDepth;
        }

        private void ResetBranchCount()
        {
            if (m_CurrentPlayer == Piece.BLACK)
                s_BlackBranchesPruned = 0;
            else
                s_WhiteBranchesPruned = 0;
        }

        private void CheckTimelimit()
        {
            if (DateTimeOffset.Now.ToUnixTimeMilliseconds() > m_TimeLimit)
                m_TerminationFlag = true;
        }

        private void ResetPositionCount()
        {
            if (m_CurrentPlayer == Piece.BLACK)
                s_BlackPositionsEvaluated = 0;
            else
                s_WhitePositionsEvaluated = 0;
        }

    }
}