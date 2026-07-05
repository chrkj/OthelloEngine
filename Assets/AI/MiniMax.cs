using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Othello.Core;
using Othello.Utility;

namespace Othello.AI
{
    public class MiniMax : ISearchEngine
    {
        private long m_TimeLimit;
        private bool m_TerminationFlag;
        private int m_CurrentDepth;
        private int m_PositionsEvaluated;
        private int m_BranchesPruned;
        private readonly int m_MaxTime;
        private readonly int m_DepthLimit;
        private readonly bool m_MoveOrderingEnabled;
        private readonly bool m_ZobristHashingEnabled;
        private readonly bool m_IterativeDeepeningEnabled;
        private readonly Dictionary<ulong, int> m_Zobrist;

        private const long MAX_ZOBRIST_SIZE = 32L;

        public MiniMax(int depth, int timeLimit, bool moveOrderingEnabled, bool iterativeDeepeningEnabled,
            bool zobristHashingEnabled)
        {
            m_DepthLimit = depth;
            m_MaxTime = timeLimit;
            m_Zobrist = new Dictionary<ulong, int>();
            m_MoveOrderingEnabled = moveOrderingEnabled;
            m_ZobristHashingEnabled = zobristHashingEnabled;
            m_IterativeDeepeningEnabled = iterativeDeepeningEnabled;
        }

        public SearchResult StartSearch(Board board)
        {
            m_TerminationFlag = false;
            m_BranchesPruned = 0;

            var start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            m_TimeLimit = start + m_MaxTime;

            int bestEvalThisIteration = 0;
            Move bestMoveThisIteration = Move.NULLMOVE;
            if (m_IterativeDeepeningEnabled)
                IterativeSearch(board, ref bestMoveThisIteration, ref bestEvalThisIteration);
            else
            {
                m_CurrentDepth = m_DepthLimit;
                CalculateMove(board, ref bestMoveThisIteration, ref bestEvalThisIteration, m_DepthLimit);
            }

            var result = new SearchResult
            {
                BestMove = bestMoveThisIteration,
                Eval = bestEvalThisIteration,
                Depth = m_CurrentDepth,
                PositionsEvaluated = m_PositionsEvaluated,
                BranchesPruned = m_BranchesPruned,
                ZobristSize = m_Zobrist.Count,
                TimeMs = DateTimeOffset.Now.ToUnixTimeMilliseconds() - start
            };
            return result;
        }

        private void IterativeSearch(Board board, ref Move bestMoveThisIteration, ref int bestEvalThisIteration)
        {
            for (int searchDepth = 1; searchDepth < m_DepthLimit + 1; searchDepth++)
            {
                if (m_TerminationFlag)
                    break;
                m_BranchesPruned = 0;
                m_CurrentDepth = searchDepth;
                CalculateMove(board, ref bestMoveThisIteration, ref bestEvalThisIteration, searchDepth);
            }
        }

        private void CalculateMove(Board board, ref Move bestMoveThisIteration, ref int bestEvalThisIteration,
            int depth)
        {
            m_PositionsEvaluated = 0;
            var currentPlayer = board.GetCurrentPlayer();
            if (currentPlayer == Piece.BLACK)
            {
                var maxEval = int.MinValue;
                Span<Move> legalMoves = stackalloc Move[Board.MAX_LEGAL_MOVES];
                board.GenerateLegalMoves(ref legalMoves);
                SwapBestMoveToFront(bestMoveThisIteration, ref legalMoves);
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
                Span<Move> legalMoves = stackalloc Move[Board.MAX_LEGAL_MOVES];
                board.GenerateLegalMoves(ref legalMoves);
                SwapBestMoveToFront(bestMoveThisIteration, ref legalMoves);
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

            var minUtil = int.MaxValue - 1;

            Span<Move> legalMoves = stackalloc Move[Board.MAX_LEGAL_MOVES];
            board.GenerateLegalMoves(ref legalMoves);
            MoveOrdering(ref legalMoves);
            if (legalMoves.Length == 0)
                minUtil = Math.Min(minUtil, MaxValue(board, depth - 1, alpha, beta));

            foreach (var legalMove in legalMoves)
            {
                var nextState = MakeMove(board, legalMove);
                minUtil = Math.Min(minUtil, MaxValue(nextState, depth - 1, alpha, beta));
                if (minUtil <= alpha)
                {
                    m_BranchesPruned++;
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

            var maxUtil = int.MinValue + 1;

            Span<Move> legalMoves = stackalloc Move[Board.MAX_LEGAL_MOVES];
            board.GenerateLegalMoves(ref legalMoves);
            MoveOrdering(ref legalMoves);
            if (legalMoves.Length == 0)
                maxUtil = Math.Max(maxUtil, MinValue(board, depth - 1, alpha, beta));

            foreach (var legalMove in legalMoves)
            {
                var nextState = MakeMove(board, legalMove);
                maxUtil = Math.Max(maxUtil, MinValue(nextState, depth - 1, alpha, beta));
                if (maxUtil >= beta)
                {
                    m_BranchesPruned++;
                    return maxUtil;
                }
                alpha = Math.Max(alpha, maxUtil);
            }
            return maxUtil;
        }

        private int GetEval(Board board)
        {
            m_PositionsEvaluated++;

            if (m_ZobristHashingEnabled && TryGetZobristValue(board, out var zobristEval))
                return zobristEval;

            if (!board.IsTerminalBoardState())
            {
                int eval = EvaluateBoard(board);
                if (m_ZobristHashingEnabled)
                    UpdateZobrist(board, eval);
                return eval;
            }
            return EvaluateTerminalBoardState(board);
        }

        private bool TryGetZobristValue(Board board, out int zobristEval)
        {
            var boardHash = board.GetHash();
            return m_Zobrist.TryGetValue(boardHash, out zobristEval);
        }

        private void UpdateZobrist(Board board, int eval)
        {
            var boardHash = board.GetHash();
            long totalSizeInBytes = m_Zobrist.Count * (Marshal.SizeOf<ulong>() + Marshal.SizeOf<int>());

            if (totalSizeInBytes < MAX_ZOBRIST_SIZE * 1024 * 1024)
                m_Zobrist[boardHash] = eval;
        }

        private int EvaluateTerminalBoardState(Board board)
        {
            if (board.GetPieceCount(Piece.BLACK) > board.GetPieceCount(Piece.WHITE))
                return int.MaxValue - 1;
            if (board.GetPieceCount(Piece.BLACK) < board.GetPieceCount(Piece.WHITE))
                return int.MinValue + 1;
            return 0;
        }

        public static int EvaluateBoard(Board board)
        {
            int blackValue = 0;
            var positionsBlack = board.GetPieces(Piece.BLACK);
            for (int i = 0; i < positionsBlack.Count; i++)
                blackValue += Move.s_CellWeight[positionsBlack[i]];

            int whiteValue = 0;
            var positionsWhite = board.GetPieces(Piece.WHITE);
            for (int i = 0; i < positionsWhite.Count; i++)
                whiteValue += Move.s_CellWeight[positionsWhite[i]];

            return blackValue - whiteValue;
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

        private void SwapBestMoveToFront(Move bestMove, ref Span<Move> legalMoves)
        {
            if (bestMove == Move.NULLMOVE)
                return;
            var targetIndex = legalMoves.IndexOf(bestMove);
            (legalMoves[0], legalMoves[targetIndex]) = (legalMoves[targetIndex], legalMoves[0]);
        }

        private void MoveOrdering(ref Span<Move> legalMoves)
        {
            if (!m_MoveOrderingEnabled)
                return;
            legalMoves.Sort();
        }

        private void CheckTimelimit()
        {
            if (DateTimeOffset.Now.ToUnixTimeMilliseconds() > m_TimeLimit)
                m_TerminationFlag = true;
        }
    }
}
