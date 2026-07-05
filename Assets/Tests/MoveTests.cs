using System;
using NUnit.Framework;
using Othello.Core;
using Othello.Utility;

namespace Othello.Tests
{
    public class MoveTests
    {
        [Test]
        public void ToString_FormatsFileAndRank()
        {
            Assert.AreEqual("A1", new Move(0).ToString());
            Assert.AreEqual("E3", new Move(20).ToString());
            Assert.AreEqual("H8", new Move(63).ToString());
        }

        [Test]
        public void ToString_HandlesTheNullMove()
        {
            Assert.AreEqual("null", Move.NULLMOVE.ToString());
        }

        [Test]
        public void Equality_ComparesByIndex()
        {
            Assert.AreEqual(new Move(5), new Move(5));
            Assert.AreNotEqual(new Move(5), new Move(6));
            Assert.IsTrue(new Move(5) == new Move(5));
            Assert.IsTrue(new Move(5) != Move.NULLMOVE);
        }

        [Test]
        public void CompareTo_OrdersHigherCellWeightsFirst()
        {
            var corner = new Move(0);  // weight 30
            var cSquare = new Move(1); // weight -12
            Assert.Less(corner.CompareTo(cSquare), 0);
            Assert.Greater(cSquare.CompareTo(corner), 0);
        }

        [Test]
        public void SpanSort_OrdersMovesByCellWeightDescending()
        {
            Span<Move> moves = stackalloc Move[] { new Move(1), new Move(27), new Move(0) };
            moves.Sort();
            Assert.AreEqual(0, moves[0].Index);  // corner, weight 30
            Assert.AreEqual(27, moves[1].Index); // center, weight -1
            Assert.AreEqual(1, moves[2].Index);  // c-square, weight -12
        }
    }
}
