using Othello.Core;

namespace Othello.AI
{
    /// <summary>
    /// Summary of a completed search. Engines fill in the fields relevant to them;
    /// the UI renders the last result instead of polling engine internals.
    /// </summary>
    public class SearchResult
    {
        public Move BestMove = Move.NULLMOVE;
        public long TimeMs;

        // Minimax
        public int Eval;
        public int Depth;
        public int PositionsEvaluated;
        public int BranchesPruned;
        public int ZobristSize;

        // MCTS
        public int IterationsRun;
        public int SimulationsRun;
        public int NodesVisited;
        public int TreeSize;
        public double WinPrediction;
    }
}
