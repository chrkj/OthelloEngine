using System;
using System.Collections.Generic;
using System.Linq;
using Othello.Core;
using Console = Othello.Core.Console;

namespace Othello.AI
{
    public class MiniMax : ISearchEngine
    {
        private readonly int m_DepthLimit;
        private readonly int m_MaxTime;
        private const int MAX_PLAYER = Piece.Black;
        private const int MIN_PLAYER = Piece.White;
        private long m_TimeLimit;
        private static int m_CurrentPlayer;
        private readonly bool m_IterativeDeepningEnabled;
        private readonly bool m_MoveOrderingEnabled;
        private readonly bool m_ZobristHashingEnabled;
        private bool m_TerminationFlag;
        private readonly Dictionary<ulong, int> m_Zobrist;
        private readonly Dictionary<ulong, Move[]> m_ZobristMoves;

        public static int s_CurrentDepthWhite;
        public static int s_CurrentDepthBlack;
        public static int s_WhitePositionsEvaluated;
        public static int s_BlackPositionsEvaluated;
        public static int s_WhiteBranchesPruned;
        public static int s_BlackBranchesPruned;
        public static int s_WhiteZobristSize;
        public static int s_BlackZobristSize;

        public MiniMax(int depth, int timeLimit, bool moveOrderingEnabled, bool iterativeDeepningEnabled, bool zobristHashingEnabled)
        {
            m_IterativeDeepningEnabled = iterativeDeepningEnabled;
            m_MoveOrderingEnabled = moveOrderingEnabled;
            m_ZobristHashingEnabled = zobristHashingEnabled;
            m_DepthLimit = depth;
            m_MaxTime = timeLimit;
            m_Zobrist = new Dictionary<ulong, int>();
            m_ZobristMoves = new Dictionary<ulong, Move[]>();
            s_WhiteZobristSize = 0;
            s_BlackZobristSize = 0;
        }

        public Move StartSearch(Board board)
        {
            m_TerminationFlag = false;
            m_CurrentPlayer = board.GetCurrentPlayer() == Piece.Black ? Piece.Black : Piece.White;
            ResetBranchCount();

            var start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            m_TimeLimit = start + m_MaxTime;

            int bestEvalThisIteration = 0;
            Move bestMoveThisIteration = board.GenerateLegalMoves().First();
            if (m_IterativeDeepningEnabled)
            {
                for (int searchDepth = 1; searchDepth < m_DepthLimit + 1; searchDepth++)
                {
                    ResetBranchCount();
                    UpdateSearchDepth(board, searchDepth);
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

        private Move CalculateMove(Board board, ref Move bestMoveThisIteration, ref int bestEvalThisIteration, int depth)
        {
            ResetPositionCount();
            var currentPlayer = board.GetCurrentPlayer();
            if (currentPlayer == MAX_PLAYER)
            {
                var highestUtil = int.MinValue;
                var legalMoves = board.GenerateLegalMoves();
                MoveOrdering(bestMoveThisIteration, ref legalMoves);
                foreach (var legalMove in legalMoves)
                {
                    if (m_TerminationFlag)
                        break;
                    var possibleNextState = MakeMove(board, legalMove);
                    bestEvalThisIteration = MinValue(possibleNextState, depth - 1, int.MinValue, int.MaxValue);
                    if (bestEvalThisIteration <= highestUtil)
                        continue;
                    highestUtil = bestEvalThisIteration;
                    bestMoveThisIteration = legalMove;
                }
            }
            else
            {
                var minUtil = int.MaxValue;
                var legalMoves = board.GenerateLegalMoves();
                MoveOrdering(bestMoveThisIteration, ref legalMoves);
                foreach (var legalMove in legalMoves)
                {
                    var possibleNextState = MakeMove(board, legalMove);
                    bestEvalThisIteration = MaxValue(possibleNextState, depth - 1, int.MinValue, int.MaxValue);
                    if (bestEvalThisIteration >= minUtil)
                        continue;
                    minUtil = bestEvalThisIteration;
                    bestMoveThisIteration = legalMove;
                }
            }
            return bestMoveThisIteration;
        }

        private int MinValue(Board board, int depth, int alpha, int beta)
        {
            CheckTimelimit();
            if (IsTerminal(board, depth))
                return GetEval(board);
            if (m_TerminationFlag)
                return int.MaxValue;

            var minUtil = int.MaxValue - 1;

            var legalMoves = GenerateLegalMoves(board);
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

            var legalMoves = GenerateLegalMoves(board);
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

            // Check for zobrist eval
            var boardHash = board.GetHash();
            if (m_ZobristHashingEnabled && m_Zobrist.TryGetValue(boardHash, out var zobristEval))
                return zobristEval;

            
            if (!board.IsTerminalBoardState())
            {
                int eval = EvaluateBoard(board);
                if (m_ZobristHashingEnabled)
                {
                    m_Zobrist[boardHash] = eval;
                    IncrementZobristCount(1);
                }
                return eval;
            }
            
            if (board.GetPieceCount(MAX_PLAYER) > board.GetPieceCount(MIN_PLAYER))
                return int.MaxValue - 1;;
            if (board.GetPieceCount(MAX_PLAYER) < board.GetPieceCount(MIN_PLAYER))
                return int.MinValue + 1;;
            return 0;
        }

        private static void IncrementZobristCount(int count)
        {
            if (m_CurrentPlayer == Piece.Black)
                s_BlackZobristSize += count;
            else
                s_WhiteZobristSize += count;
        }

        private static void IncrementPositionCount()
        {
            if (m_CurrentPlayer == Piece.Black)
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
            nextBoardState.ChangePlayer();
            return nextBoardState;
        }

        private static int EvaluateBoard(Board board)
        {
            int value = 0;
            var positions = board.GetPiecePositionsBlack();
            foreach (var pos in positions)
                value += Move.m_cellWeight[pos];
            return value;
        }

        private static void PrintSearchData(Board board, long start, Move bestMove)
        {
            var end = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            Console.Log(board.GetCurrentPlayerAsString() + " plays " + bestMove);
            Console.Log("Search time: " + (end - start) + " ms");
            Console.Log("Positions examined: " + (m_CurrentPlayer == Piece.Black ? s_BlackPositionsEvaluated : s_WhitePositionsEvaluated));
            Console.Log("----------------------------------------------------");
        }

        private void MoveOrdering(Move bestMove, ref Move[] legalMoves)
        {
            if (!m_MoveOrderingEnabled)
                return;
            
            Array.Sort(legalMoves);
            if (bestMove == null)
                return;

            int targetIndex = Array.IndexOf(legalMoves, bestMove);
            if (targetIndex == -1) 
                return;
            
            // Swap the bestMove to the front of the array
            (legalMoves[0], legalMoves[targetIndex]) = (legalMoves[targetIndex], legalMoves[0]);
        }

        private static void IncrementPruneCount()
        {
            if (m_CurrentPlayer == Piece.Black)
                s_BlackBranchesPruned++;
            else
                s_WhiteBranchesPruned++;
        }

        private static void UpdateSearchDepth(Board board, int searchDepth)
        {
            if (board.GetCurrentPlayer() == Piece.White)
                s_CurrentDepthWhite = searchDepth;
            else
                s_CurrentDepthBlack = searchDepth;
        }

        private static void ResetBranchCount()
        {
            if (m_CurrentPlayer == Piece.Black)
                s_BlackBranchesPruned = 0;
            else
                s_WhiteBranchesPruned = 0;
        }

        private void CheckTimelimit()
        {
            if (DateTimeOffset.Now.ToUnixTimeMilliseconds() > m_TimeLimit)
                m_TerminationFlag = true;
        }

        private Move[] GenerateLegalMoves(Board board)
        {
            Move[] legalMoves;
            if (m_ZobristHashingEnabled)
            {
                var boardHash = board.GetHash();
                if (m_ZobristMoves.TryGetValue(boardHash, out var move))
                {
                    legalMoves = new Move[move.Length];
                    Array.Copy(move, legalMoves, legalMoves.Length);
                }
                else
                {
                    legalMoves = board.GenerateLegalMoves();
                    Array.Sort(legalMoves);
                    m_ZobristMoves[boardHash] = new Move[legalMoves.Length];
                    Array.Copy(legalMoves, m_ZobristMoves[boardHash], legalMoves.Length);
                    IncrementZobristCount(legalMoves.Length);
                }
            }
            else
            {
                legalMoves = board.GenerateLegalMoves();
            }

            return legalMoves;
        }

        private static void ResetPositionCount()
        {
            if (m_CurrentPlayer == Piece.Black)
                s_BlackPositionsEvaluated = 0;
            else
                s_WhitePositionsEvaluated = 0;
        }

    }
}