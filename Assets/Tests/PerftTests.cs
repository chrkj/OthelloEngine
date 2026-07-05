using System;
using NUnit.Framework;
using Othello.Core;

namespace Othello.Tests
{
    public class PerftTests
    {
        // Known node counts for Othello from the standard start position.
        // No passes or finished games occur within the first 8 plies, so these
        // values are independent of pass-counting conventions.
        private static readonly long[] s_ExpectedLeafNodes = { 1, 4, 12, 56, 244, 1396, 8200, 55092, 390216 };

        [Test]
        public void Perft_MatchesKnownNodeCounts_UpToDepth6()
        {
            var board = CreateStartBoard();
            for (var depth = 1; depth <= 6; depth++)
                Assert.AreEqual(s_ExpectedLeafNodes[depth], Perft(board, depth), $"perft({depth}) mismatch");
        }

        [Test]
        public void Perft_MatchesKnownNodeCounts_AtDepth7And8()
        {
            var board = CreateStartBoard();
            Assert.AreEqual(s_ExpectedLeafNodes[7], Perft(board, 7), "perft(7) mismatch");
            Assert.AreEqual(s_ExpectedLeafNodes[8], Perft(board, 8), "perft(8) mismatch");
        }

        private static Board CreateStartBoard()
        {
            var board = new Board();
            board.LoadStartPosition();
            return board;
        }

        private static long Perft(Board board, int depth)
        {
            if (depth == 0)
                return 1;

            Span<Move> legalMoves = stackalloc Move[Board.MAX_LEGAL_MOVES];
            board.GenerateLegalMoves(ref legalMoves);

            if (legalMoves.Length == 0)
            {
                if (board.IsTerminalBoardState())
                    return 1;
                var passed = board.Copy();
                passed.ChangePlayer();
                return Perft(passed, depth); // a pass does not consume a ply
            }

            long nodes = 0;
            foreach (var move in legalMoves)
            {
                var nextState = board.Copy();
                nextState.MakeMove(move);
                nextState.ChangePlayer();
                nodes += Perft(nextState, depth - 1);
            }
            return nodes;
        }
    }
}
