using System;
using System.Collections.Generic;
using System.Linq;
using Othello.Core;
using Console = Othello.Core.Console;

namespace Othello.AI
{
    public class MiniMax : ISearchEngine
    {
        private readonly int m_depthLimit;
        private readonly int m_maxTime;
        private const int m_ParityWeight = 1;
        private const int m_CornerWeight = 4;
        private const int m_MaxPlayer = Piece.Black;
        private const int m_MinPlayer = Piece.White;
        private long m_TimeLimit;
        private static int m_CurrentPlayer;
        private readonly bool m_IterativeDeepningEnabled;
        private readonly bool m_MoveOrderingEnabled;
        private readonly bool m_ZobristHashingEnabled;
        private bool m_TerminationFlag;
        private readonly Dictionary<ulong, int> m_Zobrist;
        private readonly Dictionary<ulong, List<Move>> m_ZobristMoves;

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
            m_depthLimit = depth;
            m_maxTime = timeLimit;
            m_Zobrist = new Dictionary<ulong, int>();
            m_ZobristMoves = new Dictionary<ulong, List<Move>>();
            s_WhiteZobristSize = 0;
            s_BlackZobristSize = 0;
        }

        public Move StartSearch(Board board)
        {
            m_TerminationFlag = false;
            m_CurrentPlayer = board.GetCurrentPlayer() == Piece.Black ? Piece.Black : Piece.White;
            ResetBranchCount();

            var start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            m_TimeLimit = start + m_maxTime;

            int bestEvalThisIteration = 0;
            Move bestMoveThisIteration = board.GenerateLegalMoves().First();
            if (m_IterativeDeepningEnabled)
            {
                for (int searchDepth = 1; searchDepth < m_depthLimit + 1; searchDepth++)
                {
                    ResetBranchCount();
                    UpdateSeatchDepth(board, searchDepth);
                    CalculateMove(board, ref bestMoveThisIteration, ref bestEvalThisIteration, searchDepth);
                    if (m_TerminationFlag)
                        break;
                }
            }
            else
            {
                CalculateMove(board, ref bestMoveThisIteration, ref bestEvalThisIteration, m_depthLimit);
            }
            PrintSearchData(board, start, bestMoveThisIteration);
            return bestMoveThisIteration;
        }

        private Move CalculateMove(Board board, ref Move bestMoveThisIteration, ref int bestEvalThisIteration, int depth)
        {
            ResetPositionCount();
            var currentPlayer = board.GetCurrentPlayer();
            if (currentPlayer == m_MaxPlayer)
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
            if (legalMoves.Count == 0)
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
            if (legalMoves.Count == 0)
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
            if (m_ZobristHashingEnabled && m_Zobrist.ContainsKey(boardHash))
                return m_Zobrist[boardHash];

            if (!board.IsTerminalBoardState())
            {
                var util = GetBoardUtility(board);
                if (m_ZobristHashingEnabled)
                {
                    m_Zobrist[boardHash] = util;
                    IncrementZobristCount(1);
                }
                return util;
            }
            if (board.GetPieceCount(m_MaxPlayer) > board.GetPieceCount(m_MinPlayer))
            {
                var util = int.MaxValue - 1;
                if (m_ZobristHashingEnabled)
                    m_Zobrist[boardHash] = util;
                return util;
            }
            if (board.GetPieceCount(m_MaxPlayer) < board.GetPieceCount(m_MinPlayer))
            {
                var util = int.MinValue + 1;
                if (m_ZobristHashingEnabled)
                    m_Zobrist[boardHash] = util;
                return util;
            }
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

        private static void PrintSearchData(Board board, long start, Move bestMove)
        {
            var end = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            Console.Log(board.GetCurrentPlayerAsString() + " plays " + bestMove.ToString());
            Console.Log("Search time: " + (end - start) + " ms");
            Console.Log("Positions examined: " + (m_CurrentPlayer == Piece.Black ? s_BlackPositionsEvaluated : s_WhitePositionsEvaluated));
            Console.Log("----------------------------------------------------");
        }

        private void MoveOrdering(Move bestMove, ref List<Move> legalMoves)
        {
            if (!m_MoveOrderingEnabled)
                return;

            legalMoves.Sort();
            if (bestMove == null)
                return;
            Move targetMove = legalMoves.Find(move => move.Equals(bestMove));
            if (targetMove != null)
            {
                int targetIndex = legalMoves.IndexOf(targetMove);
                if (targetIndex != -1)
                {
                    legalMoves.RemoveAt(targetIndex);
                    legalMoves.Insert(0, targetMove);
                }
            }
        }

        private static void IncrementPruneCount()
        {
            if (m_CurrentPlayer == Piece.Black)
                s_BlackBranchesPruned++;
            else
                s_WhiteBranchesPruned++;
        }

        private static void UpdateSeatchDepth(Board board, int searchDepth)
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

        private List<Move> GenerateLegalMoves(Board board)
        {
            List<Move> legalMoves;
            if (m_ZobristHashingEnabled)
            {
                var boardHash = board.GetHash();
                if (m_ZobristMoves.ContainsKey(boardHash))
                {
                    legalMoves = new List<Move>(m_ZobristMoves[boardHash]);
                }
                else
                {
                    legalMoves = board.GenerateLegalMoves();
                    legalMoves.Sort();
                    m_ZobristMoves[boardHash] = new List<Move>(legalMoves);
                    IncrementZobristCount(legalMoves.Count);
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