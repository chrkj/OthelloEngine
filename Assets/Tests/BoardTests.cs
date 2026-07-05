using System;
using NUnit.Framework;
using Othello.Core;

namespace Othello.Tests
{
    public class BoardTests
    {
        // Black's legal moves from the start position: E3, F4, C5, D6
        private static readonly int[] s_StartPositionMoves = { 20, 29, 34, 43 };

        private static readonly ulong s_StartBlackPieces = (1UL << 27) | (1UL << 36);
        private static readonly ulong s_StartWhitePieces = (1UL << 28) | (1UL << 35);

        private static Board CreateStartBoard()
        {
            var board = new Board();
            board.LoadStartPosition();
            return board;
        }

        private static int[] GetLegalMoveIndices(Board board)
        {
            Span<Move> legalMoves = stackalloc Move[Board.MAX_LEGAL_MOVES];
            board.GenerateLegalMoves(ref legalMoves);
            var indices = new int[legalMoves.Length];
            for (var i = 0; i < legalMoves.Length; i++)
                indices[i] = legalMoves[i].Index;
            return indices;
        }

        [Test]
        public void StartPosition_HasTwoPiecesPerPlayerAndBlackToMove()
        {
            var board = CreateStartBoard();
            Assert.AreEqual(2, board.GetPieceCount(Piece.BLACK));
            Assert.AreEqual(2, board.GetPieceCount(Piece.WHITE));
            Assert.AreEqual(Piece.BLACK, board.GetCurrentPlayer());
        }

        [Test]
        public void StartPosition_HasFourLegalMoves()
        {
            CollectionAssert.AreEquivalent(s_StartPositionMoves, GetLegalMoveIndices(CreateStartBoard()));
        }

        [Test]
        public void MakeMove_PlacesPieceAndFlipsCapturedPiece()
        {
            var board = CreateStartBoard();
            board.MakeMove(new Move(29)); // black plays F4, capturing the white piece on E4 (28)

            Assert.AreEqual(4, board.GetPieceCount(Piece.BLACK));
            Assert.AreEqual(1, board.GetPieceCount(Piece.WHITE));
            Assert.AreEqual(Piece.BLACK, board.GetPieceColor(5, 3)); // the placed piece
            Assert.AreEqual(Piece.BLACK, board.GetPieceColor(4, 3)); // the flipped piece
        }

        [Test]
        public void Copy_IsIndependentOfTheOriginal()
        {
            var board = CreateStartBoard();
            var copy = board.Copy();
            Assert.IsTrue(board.Equals(copy));

            copy.MakeMove(new Move(29));
            copy.ChangePlayer();

            Assert.IsFalse(board.Equals(copy));
            Assert.AreEqual(2, board.GetPieceCount(Piece.BLACK));
        }

        [Test]
        public void GetBoardState_ReturnsRunning_ForTheStartPosition()
        {
            Assert.AreEqual(-1, CreateStartBoard().GetBoardState());
        }

        [Test]
        public void BoardWithoutOpponentPieces_IsTerminalAndWonByBlack()
        {
            var board = new Board(blackPieces: 1UL, whitePieces: 0UL, isWhiteToMove: false);
            Assert.IsTrue(board.IsTerminalBoardState());
            Assert.AreEqual(Piece.BLACK, board.GetWinner());
            Assert.AreEqual(Piece.BLACK, board.GetBoardState());
        }

        [Test]
        public void PopCount_CountsSetBits()
        {
            Assert.AreEqual(0, Board.PopCount(0UL));
            Assert.AreEqual(1, Board.PopCount(1UL << 63));
            Assert.AreEqual(2, Board.PopCount(0x8000000000000001UL));
            Assert.AreEqual(32, Board.PopCount(0x5555555555555555UL));
            Assert.AreEqual(64, Board.PopCount(ulong.MaxValue));
        }

        [Test]
        public void GetHash_IsEqualForIdenticalPositions()
        {
            Assert.AreEqual(CreateStartBoard().GetHash(), CreateStartBoard().GetHash());
        }

        [Test]
        public void GetHash_DiffersBySideToMove()
        {
            var blackToMove = new Board(s_StartBlackPieces, s_StartWhitePieces, isWhiteToMove: false);
            var whiteToMove = new Board(s_StartBlackPieces, s_StartWhitePieces, isWhiteToMove: true);
            Assert.AreNotEqual(blackToMove.GetHash(), whiteToMove.GetHash());
        }

        [Test]
        public void GetHash_DistinguishesColorSwappedPositions()
        {
            var original = new Board(s_StartBlackPieces, s_StartWhitePieces, isWhiteToMove: false);
            var colorSwapped = new Board(s_StartWhitePieces, s_StartBlackPieces, isWhiteToMove: false);
            Assert.AreNotEqual(original.GetHash(), colorSwapped.GetHash());
        }
    }
}
