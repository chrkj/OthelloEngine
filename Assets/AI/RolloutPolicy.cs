using System;

using Othello.Core;

namespace Othello.AI
{
    /// <summary>
    /// Chooses moves during an MCTS rollout. Uniform-random by default; when heuristic rollouts are
    /// enabled it plays epsilon-greedy on the positional cell-weight table — mostly the highest-weighted
    /// square (grab corners, avoid X-squares), with an epsilon chance of a random move so rollouts don't
    /// collapse to a single deterministic line. Extension point for future rollout policies.
    /// </summary>
    public class RolloutPolicy
    {
        public bool UseHeuristic { get; }
        public float Epsilon { get; }

        public RolloutPolicy(bool useHeuristic = false, float epsilon = 0.2f)
        {
            UseHeuristic = useHeuristic;
            Epsilon = epsilon < 0f ? 0f : (epsilon > 1f ? 1f : epsilon);
        }

        public Move Pick(ReadOnlySpan<Move> legalMoves, Random rng)
        {
            if (!UseHeuristic || rng.NextDouble() < Epsilon)
                return legalMoves[rng.Next(legalMoves.Length)];

            var best = legalMoves[0];
            for (var i = 1; i < legalMoves.Length; i++)
                if (Move.s_CellWeight[legalMoves[i].Index] > Move.s_CellWeight[best.Index])
                    best = legalMoves[i];
            return best;
        }
    }
}
