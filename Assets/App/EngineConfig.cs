using System;
using UnityEngine;

using Othello.AI;

namespace Othello.App
{
    public enum EngineKind { Random = 0, Minimax = 1, Mcts = 2 }

    /// <summary>An editable description of one AI engine, convertible to an Arena entrant.</summary>
    public class EngineConfig
    {
        public EngineKind Kind = EngineKind.Mcts;
        public int Depth = 6;
        public int Iterations = 5000;
        public int TimeLimitMs = 1000;
        public MctsType MctsVariant = MctsType.Sequential;
        public bool MoveOrdering = true;
        public bool IterativeDeepening;
        public bool ZobristHashing;

        public bool RequiresGpu => Kind == EngineKind.Mcts && MctsVariant == MctsType.GpuParallel;

        public string DisplayName()
        {
            switch (Kind)
            {
                case EngineKind.Random:
                    return "Random";
                case EngineKind.Minimax:
                    var flags = (MoveOrdering ? "+ord" : "") + (IterativeDeepening ? "+id" : "") +
                                (ZobristHashing ? "+zb" : "");
                    return $"Minimax d{Depth}{flags}";
                case EngineKind.Mcts:
                    return $"MCTS {MctsVariant} {Iterations}";
                default:
                    return "Engine";
            }
        }

        public EngineEntry ToEntry(string uniqueName, ComputeShader gpuShader)
        {
            switch (Kind)
            {
                case EngineKind.Random:
                    return new EngineEntry(uniqueName, () => new RandomPlay());
                case EngineKind.Minimax:
                    return new EngineEntry(uniqueName,
                        () => new MiniMax(Mathf.Max(1, Depth), TimeLimitMs, MoveOrdering, IterativeDeepening, ZobristHashing));
                case EngineKind.Mcts:
                    var gpu = RequiresGpu;
                    return new EngineEntry(uniqueName,
                        () => new Mcts(Mathf.Max(1, Iterations), TimeLimitMs, MctsVariant, gpu ? gpuShader : null));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
