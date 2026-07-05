using System;
using System.Threading;
using System.Threading.Tasks;

using Othello.Core;
using UnityEngine;
using Random = System.Random;

namespace Othello.AI
{
    public enum MctsType { Sequential = 0, RootParallel = 1, TreeParallel = 2, GpuParallel = 3 }

    public class Mcts : ISearchEngine
    {
        private const int BLOCK_SIZE = 50;
        private const int IS_RUNNING = -1;
        private const int NUM_GPU_SIMS = 100;

        private Node m_CachedNode;
        private int m_NodesVisited;
        private int m_IterationsRun;
        private int m_SimulationsRun;
        private int m_NumTrees;

        private readonly int m_MaxTime;
        private readonly int m_MaxIterations;
        public readonly MctsType Variant;

        private Random m_Rng;
        private readonly ComputeShader m_ComputeShader;

        private ComputeBuffer m_PieceBuffer;
        private ComputeBuffer m_CurrentPlayerBuffer;
        private ComputeBuffer m_SeedBuffer;
        private ComputeBuffer m_WinBuffer;
        private ComputeBuffer m_DrawBuffer;

        private readonly int m_DrawsId = Shader.PropertyToID("_Draws");
        private readonly int m_WinsId = Shader.PropertyToID("_Wins");
        private readonly int m_SeedId = Shader.PropertyToID("_Seed");
        private readonly int m_CurrentPlayerId = Shader.PropertyToID("_CurrentPlayer");
        private readonly int m_PiecesId = Shader.PropertyToID("_Pieces");

        public Mcts(int maxIterations, int maxTime, MctsType mctsType, ComputeShader computeShader = null)
        {
            m_MaxTime = maxTime;
            m_MaxIterations = maxIterations;
            Variant = mctsType;
            m_ComputeShader = computeShader;
            m_Rng = new Random();
        }

        public SearchResult StartSearch(Board board)
        {
            m_NodesVisited = 0;
            m_IterationsRun = 0;
            m_SimulationsRun = 0;

            var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var timeLimit = startTime + m_MaxTime;
            var rootNode = SetRootNode(board);

            switch (Variant)
            {
                case MctsType.Sequential:
                    RunSequential(rootNode, timeLimit);
                    break;
                case MctsType.RootParallel:
                    RunRootParallel(rootNode, timeLimit);
                    break;
                case MctsType.TreeParallel:
                    RunTreeParallel(rootNode, timeLimit);
                    break;
                case MctsType.GpuParallel:
                    RunGpuParallel(rootNode, timeLimit);
                    break;
                default:
                    throw new NotImplementedException();
            }

            var (bestNode, bestMove) = rootNode.SelectBestNode();
            m_CachedNode = bestNode;
            m_CachedNode.Parent = null;
            var endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            var result = new SearchResult
            {
                BestMove = bestMove,
                TimeMs = endTime - startTime,
                IterationsRun = m_IterationsRun,
                SimulationsRun = m_SimulationsRun,
                NodesVisited = m_NodesVisited,
                TreeSize = (int)bestNode.NumVisits,
                WinPrediction = bestNode.NumVisits > 0 ? bestNode.NumWins / bestNode.NumVisits * 100 : 0
            };
            return result;
        }

        private void RunSequential(Node rootNode, long timeLimit)
        {
            for (var iterations = 0;
                 (iterations < m_MaxIterations) & DateTimeOffset.Now.ToUnixTimeMilliseconds() < timeLimit;
                 iterations += BLOCK_SIZE)
            {
                for (var i = 0; i < BLOCK_SIZE; i++)
                {
                    var promisingNode = Select(rootNode);
                    Expand(promisingNode);

                    var nodeToExplore = promisingNode;
                    if (promisingNode.Children.Count > 0)
                        nodeToExplore = promisingNode.GetRandomChildNode();
                    var winningPlayer = Simulate(nodeToExplore);

                    BackPropagation(nodeToExplore, winningPlayer);
                    Interlocked.Increment(ref m_IterationsRun);
                }
            }
        }

        private void RunRootParallel(Node rootNode, long timeLimit)
        {
            Parallel.For(0, m_MaxIterations + 1,
                (_, loopState) =>
                {
                    Node promisingNode;
                    lock (this)
                    {
                        promisingNode = Select(rootNode);
                        Expand(promisingNode);
                    }

                    var nodeToExplore = promisingNode;
                    if (promisingNode.Children.Count > 0)
                        nodeToExplore = promisingNode.GetRandomChildNode();
                    var winningPlayer = Simulate(nodeToExplore);

                    BackPropagation(nodeToExplore, winningPlayer);
                    Interlocked.Increment(ref m_IterationsRun);

                    if (DateTimeOffset.Now.ToUnixTimeMilliseconds() > timeLimit)
                        loopState.Break();
                }
            );
        }

        private void RunTreeParallel(Node rootNode, long timeLimit)
        {
            if (rootNode.Children.Count == 0)
                Expand(rootNode);
            m_NumTrees = rootNode.Children.Count;

            Task[] tasks = new Task[rootNode.Children.Count];
            for (int i = 0; i < rootNode.Children.Count; i++)
            {
                var treeNode = rootNode.Children[i];
                Expand(treeNode);
                tasks[i] = Task.Factory.StartNew(() => { RunTree(treeNode, timeLimit); },
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            }

            Task.WaitAll(tasks);
        }

        private void RunGpuParallel(Node rootNode, long timeLimit)
        {
            InitializeBuffers();
            for (var iterations = 0;
                 (iterations < m_MaxIterations) & DateTimeOffset.Now.ToUnixTimeMilliseconds() < timeLimit;
                 iterations += BLOCK_SIZE)
            {
                for (var i = 0; i < BLOCK_SIZE; i++)
                {
                    var promisingNode = Select(rootNode);
                    Expand(promisingNode);

                    var nodeToExplore = promisingNode;
                    if (promisingNode.Children.Count > 0)
                        nodeToExplore = promisingNode.GetRandomChildNode();
                    var winningPlayer = SimulateGpu(nodeToExplore);

                    BackPropagation(nodeToExplore, winningPlayer);
                    Interlocked.Increment(ref m_IterationsRun);
                }
            }
            ReleaseBuffers();
        }

        private void RunTree(Node rootNode, long timeLimit)
        {
            for (var iterations = 0;
                 (iterations < m_MaxIterations / m_NumTrees) & DateTimeOffset.Now.ToUnixTimeMilliseconds() < timeLimit;
                 iterations += BLOCK_SIZE)
            {
                for (var i = 0; i < BLOCK_SIZE; i++)
                {
                    var promisingNode = Select(rootNode);
                    Expand(promisingNode);

                    var nodeToExplore = promisingNode;
                    if (promisingNode.Children.Count > 0)
                        nodeToExplore = promisingNode.GetRandomChildNode();
                    var winningPlayer = Simulate(nodeToExplore);

                    BackPropagation(nodeToExplore, winningPlayer);
                    Interlocked.Increment(ref m_IterationsRun);
                }
            }
        }

        private Node SetRootNode(Board board)
        {
            Node rootNode = null;
            if (m_CachedNode != null)
            {
                foreach (var node in m_CachedNode.Children)
                    if (node.Board.Equals(board))
                    {
                        rootNode = node;
                        break;
                    }
            }
            rootNode ??= new Node(board.Copy());
            return rootNode;
        }

        private static Node Select(Node node)
        {
            var currentNode = node;
            while (currentNode.Children.Count > 0)
                currentNode = SelectNodeWithUct(currentNode);
            return currentNode;
        }

        private static void Expand(Node node)
        {
            var childNodes = node.CreateChildNodes();
            foreach (var childNode in childNodes)
            {
                childNode.Parent = node;
                node.Children.Add(childNode);
            }
        }

        private static void BackPropagation(Node nodeToExplore, int winningPlayer)
        {
            var currentNode = nodeToExplore;
            while (currentNode != null)
            {
                double simulationScore = (winningPlayer == currentNode.Board.GetCurrentPlayer()) ? 0 : 1;
                if (winningPlayer == 0)
                    simulationScore = 0.5;
                currentNode.NumVisits++;
                currentNode.NumWins += (winningPlayer == currentNode.Board.GetCurrentPlayer()) ? 0 : 1;
                currentNode.Score += simulationScore;
                currentNode = currentNode.Parent;
            }
        }

        private int Simulate(Node node)
        {
            var tempNode = node.Copy();
            var winningPlayer = tempNode.Board.GetBoardState();
            while (winningPlayer == IS_RUNNING)
            {
                tempNode.RandomMove();
                Interlocked.Increment(ref m_NodesVisited);
                winningPlayer = tempNode.Board.GetBoardState();
            }
            Interlocked.Increment(ref m_SimulationsRun);
            return winningPlayer;
        }

        private int SimulateGpu(Node node)
        {
            var blackPieces = node.Board.GetPiecesBitBoard(Piece.BLACK);
            var whitePieces = node.Board.GetPiecesBitBoard(Piece.WHITE);
            var currentPlayer = node.Board.GetCurrentPlayer() == 2 ? 0 : 1;

            // Input buffers
            m_PieceBuffer.SetData(new[] { blackPieces, whitePieces });
            m_ComputeShader.SetBuffer(0, m_PiecesId, m_PieceBuffer);
            m_CurrentPlayerBuffer.SetData(new[] { currentPlayer });
            m_ComputeShader.SetBuffer(0, m_CurrentPlayerId, m_CurrentPlayerBuffer);
            m_SeedBuffer.SetData(new[] { m_Rng.Next() });
            m_ComputeShader.SetBuffer(0, m_SeedId, m_SeedBuffer);

            // Output buffers
            m_ComputeShader.SetBuffer(0, m_WinsId, m_WinBuffer);
            m_ComputeShader.SetBuffer(0, m_DrawsId, m_DrawBuffer);

            m_ComputeShader.Dispatch(0, 1, 1, 1);

            var simWins = new int[1];
            m_WinBuffer.GetData(simWins);
            var simDraws = new int[1];
            m_DrawBuffer.GetData(simDraws);

            int winningPlayer;
            if (simWins[0] > (NUM_GPU_SIMS - simWins[0] - simDraws[0]))
                winningPlayer = node.Board.GetCurrentOpponent();
            else if (simWins[0] == (NUM_GPU_SIMS - simWins[0] - simDraws[0]))
                winningPlayer = 0;
            else
                winningPlayer = node.Board.GetCurrentPlayer();

            Interlocked.Add(ref m_SimulationsRun, NUM_GPU_SIMS);
            return winningPlayer;
        }

        private static Node SelectNodeWithUct(Node node)
        {
            var selectedNode = node.Children[0];
            for (var i = 1; i < node.Children.Count; i++)
            {
                var currentNode = node.Children[i];
                if (currentNode.CalculateUct() > selectedNode.CalculateUct())
                    selectedNode = currentNode;
            }

            return selectedNode;
        }

        private void InitializeBuffers()
        {
            m_PieceBuffer = new ComputeBuffer(2, sizeof(ulong), ComputeBufferType.Default);
            m_CurrentPlayerBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Default);
            m_SeedBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Default);
            m_WinBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Default);
            m_DrawBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Default);
        }

        private void ReleaseBuffers()
        {
            m_PieceBuffer.Release();
            m_CurrentPlayerBuffer.Release();
            m_SeedBuffer.Release();
            m_WinBuffer.Release();
            m_DrawBuffer.Release();
        }
    }
}
