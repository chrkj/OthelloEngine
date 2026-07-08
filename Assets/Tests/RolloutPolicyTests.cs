using System;
using NUnit.Framework;
using Othello.AI;
using Othello.Core;

namespace Othello.Tests
{
    public class RolloutPolicyTests
    {
        [Test]
        public void Heuristic_WithZeroEpsilon_PicksHighestCellWeightMove()
        {
            // A1 (index 0) is a corner (weight 30); B1 (1) is an X/c-square (-12); C1 (2) is 0.
            var moves = new[] { new Move(1), new Move(2), new Move(0) };
            var policy = new RolloutPolicy(useHeuristic: true, epsilon: 0f);

            // Epsilon 0 makes the choice deterministic regardless of the RNG.
            Assert.AreEqual(0, policy.Pick(moves, new Random(0)).Index);
            Assert.AreEqual(0, policy.Pick(moves, new Random(999)).Index);
        }

        [Test]
        public void Uniform_AlwaysReturnsALegalMove()
        {
            var moves = new[] { new Move(20), new Move(29), new Move(34) };
            var policy = new RolloutPolicy(useHeuristic: false);
            var rng = new Random(1);
            for (var i = 0; i < 50; i++)
                CollectionAssert.Contains(new[] { 20, 29, 34 }, policy.Pick(moves, rng).Index);
        }

        [Test]
        public void Epsilon_IsClampedToUnitRange()
        {
            Assert.AreEqual(0f, new RolloutPolicy(true, -1f).Epsilon);
            Assert.AreEqual(1f, new RolloutPolicy(true, 5f).Epsilon);
        }
    }
}
