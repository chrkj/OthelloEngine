using NUnit.Framework;
using Othello.AI;
using Othello.Core;

namespace Othello.Tests
{
    public class SearchEngineTests
    {
        private static readonly int[] s_StartPositionMoves = { 20, 29, 34, 43 };

        private static Board CreateStartBoard()
        {
            var board = new Board();
            board.LoadStartPosition();
            return board;
        }

        // A1 is black, B1 is white; black's only legal move is C1 (index 2), which wipes out white
        private static Board CreateWipeoutBoard()
        {
            return new Board(blackPieces: 1UL, whitePieces: 1UL << 1, isWhiteToMove: false);
        }

        [Test]
        public void MiniMax_ReturnsLegalMove_FromStartPosition()
        {
            var engine = new MiniMax(depth: 3, timeLimit: 60000, moveOrderingEnabled: false,
                iterativeDeepeningEnabled: false, zobristHashingEnabled: false);
            var result = engine.StartSearch(CreateStartBoard());
            CollectionAssert.Contains(s_StartPositionMoves, result.BestMove.Index);
            Assert.Greater(result.PositionsEvaluated, 0);
        }

        [Test]
        public void MiniMax_WithAllFeaturesEnabled_ReturnsLegalMove()
        {
            var engine = new MiniMax(depth: 4, timeLimit: 60000, moveOrderingEnabled: true,
                iterativeDeepeningEnabled: true, zobristHashingEnabled: true);
            var result = engine.StartSearch(CreateStartBoard());
            CollectionAssert.Contains(s_StartPositionMoves, result.BestMove.Index);
        }

        [Test]
        public void MiniMax_FindsTheWinningMove()
        {
            var engine = new MiniMax(depth: 3, timeLimit: 60000, moveOrderingEnabled: false,
                iterativeDeepeningEnabled: false, zobristHashingEnabled: false);
            var result = engine.StartSearch(CreateWipeoutBoard());
            Assert.AreEqual(2, result.BestMove.Index);
            Assert.AreEqual(int.MaxValue - 1, result.Eval);
        }

        [Test]
        public void MiniMax_ReportsTheEvalOfTheChosenMove()
        {
            // Black on C3 (18), white on B2 (9) and B3 (17). Black has exactly two moves:
            // A1 (0) captures B2 for eval 18, A3 (16) captures B3 for eval 12.
            // The better move is searched first, so an engine that reports the eval of the
            // last searched move instead of the chosen one returns 12 here.
            var board = new Board(blackPieces: 1UL << 18, whitePieces: (1UL << 9) | (1UL << 17), isWhiteToMove: false);
            var engine = new MiniMax(depth: 1, timeLimit: 60000, moveOrderingEnabled: false,
                iterativeDeepeningEnabled: false, zobristHashingEnabled: false);

            var result = engine.StartSearch(board);

            Assert.AreEqual(0, result.BestMove.Index);
            Assert.AreEqual(18, result.Eval);
        }

        [Test]
        public void MctsSequential_ReturnsLegalMove_FromStartPosition()
        {
            var engine = new Mcts(maxIterations: 500, maxTime: 10000, MctsType.Sequential);
            var result = engine.StartSearch(CreateStartBoard());
            CollectionAssert.Contains(s_StartPositionMoves, result.BestMove.Index);
            Assert.Greater(result.SimulationsRun, 0);
        }

        [Test]
        public void MctsRootParallel_ReturnsLegalMove_FromStartPosition()
        {
            var engine = new Mcts(maxIterations: 500, maxTime: 10000, MctsType.RootParallel);
            var result = engine.StartSearch(CreateStartBoard());
            CollectionAssert.Contains(s_StartPositionMoves, result.BestMove.Index);
        }

        [Test]
        public void MctsTreeParallel_ReturnsLegalMove_FromStartPosition()
        {
            var engine = new Mcts(maxIterations: 500, maxTime: 10000, MctsType.TreeParallel);
            var result = engine.StartSearch(CreateStartBoard());
            CollectionAssert.Contains(s_StartPositionMoves, result.BestMove.Index);
        }

        [Test]
        public void MctsSequential_FindsTheOnlyLegalMove()
        {
            var engine = new Mcts(maxIterations: 100, maxTime: 10000, MctsType.Sequential);
            var result = engine.StartSearch(CreateWipeoutBoard());
            Assert.AreEqual(2, result.BestMove.Index);
        }

        [Test]
        public void MiniMax_ReturnsLegalMove_WhenTimeLimitAlreadyExpired()
        {
            // Negative time limit forces immediate termination before any move is fully searched.
            var engine = new MiniMax(depth: 8, timeLimit: -1, moveOrderingEnabled: false,
                iterativeDeepeningEnabled: false, zobristHashingEnabled: false);
            var result = engine.StartSearch(CreateStartBoard());
            Assert.AreNotEqual(Move.NULLMOVE, result.BestMove);
            CollectionAssert.Contains(s_StartPositionMoves, result.BestMove.Index);
        }

        [Test]
        public void Mcts_ReturnsLegalMove_WhenNoIterationsRun()
        {
            // Negative time limit means the search loop never runs, so the root is never expanded.
            var engine = new Mcts(maxIterations: 1000, maxTime: -1, MctsType.Sequential);
            var result = engine.StartSearch(CreateStartBoard());
            Assert.AreNotEqual(Move.NULLMOVE, result.BestMove);
            CollectionAssert.Contains(s_StartPositionMoves, result.BestMove.Index);
        }

        [Test]
        public void RandomPlay_ReturnsLegalMove_FromStartPosition()
        {
            var engine = new RandomPlay();
            var result = engine.StartSearch(CreateStartBoard());
            CollectionAssert.Contains(s_StartPositionMoves, result.BestMove.Index);
        }

        [Test]
        public void EvaluateBoard_ScoresCornersAboveAdjacentSquares()
        {
            // Black on the A1 corner (weight 30), white on the B1 c-square (weight -12)
            var board = new Board(blackPieces: 1UL, whitePieces: 1UL << 1, isWhiteToMove: false);
            Assert.AreEqual(42, MiniMax.EvaluateBoard(board));
        }

        [Test]
        public void EvaluateBoard_IsColorAntisymmetric()
        {
            var board = new Board(blackPieces: 1UL, whitePieces: 1UL << 1, isWhiteToMove: false);
            var swapped = new Board(blackPieces: 1UL << 1, whitePieces: 1UL, isWhiteToMove: false);
            Assert.AreEqual(-MiniMax.EvaluateBoard(board), MiniMax.EvaluateBoard(swapped));
        }
    }
}
