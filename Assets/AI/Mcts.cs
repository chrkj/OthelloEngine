using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Othello.UI;
using Othello.Core;
using UnityEngine;
using Random = System.Random;
using Console = Othello.Core.Console;

namespace Othello.AI
{
    public class Mcts : ISearchEngine
    {
        public static int s_WhiteIterationsRun;
        public static int s_BlackIterationsRun;
        public static int s_WhiteSimulationsRun;
        public static int s_BlackSimulationsRun;
        public static double s_WhiteWinPrediction;
        public static double s_BlackWinPrediction;

        private const int BLOCK_SIZE = 50;
        private const int IS_RUNNING = -1;
        private const int NUM_GPU_SIMS = 100;

        private Node m_CachedNode;
        private int m_NodesVisited;
        private int m_CurrentPlayer;
        private int m_NumTrees;

        private readonly int m_MaxTime;
        private readonly int m_MaxIterations;
        public readonly MenuUI.MctsType m_MctsType;
        
        private Random m_Rng;
        private ComputeShader m_ComputeShader;
        
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

        public Mcts(int maxIterations, int maxTime, MenuUI.MctsType mctsType)
        {
            m_MaxTime = maxTime;
            m_MaxIterations = maxIterations;
            s_WhiteIterationsRun = 0;
            s_BlackIterationsRun = 0;
            s_WhiteWinPrediction = 0;
            s_BlackWinPrediction = 0;
            m_MctsType = mctsType;
            m_ComputeShader = GameManager.Instance.ComputeShader;
            m_Rng = new Random();
        }

        public Move StartSearch(Board board)
        {
            m_NodesVisited = 0;
            m_CurrentPlayer = board.GetCurrentPlayer() == Piece.BLACK ? Piece.BLACK : Piece.WHITE;
            ResetSimCount();
            return m_MctsType switch
            {
                MenuUI.MctsType.Sequential => CalculateMoveSequential(board),
                MenuUI.MctsType.RootParallel => CalculateMoveRoot(board),
                MenuUI.MctsType.TreeParallel => CalculateMoveTree(board),
                MenuUI.MctsType.GpuParallel => CalculateGpu(board),
                _ => throw new NotImplementedException()
            };
        }

        private void ResetSimCount()
        {
            if (m_CurrentPlayer == Piece.BLACK)
            {
                s_BlackIterationsRun = 0;
                s_BlackSimulationsRun = 0;
            }
            else
            {
                s_WhiteIterationsRun = 0;
                s_WhiteSimulationsRun = 0;
            }
        }

        private Move CalculateMoveTree(Board board)
        {
            var rootNode = SetRootNode(board);
            if (rootNode.Children.Count == 0)
                Expand(rootNode);
            m_NumTrees = rootNode.Children.Count;

            var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var timeLimit = startTime + m_MaxTime;

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

            var (bestNode, bestMove) = rootNode.SelectBestNode();
            m_CachedNode = bestNode;
            m_CachedNode.Parent = null;
            var endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            SetWinPrediction(bestNode);
            PrintSearchData(rootNode, startTime, bestNode, bestMove, endTime);
            return bestMove;
        }

        private Move CalculateMoveRoot(Board board)
        {
            var rootNode = SetRootNode(board);
            var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var timeLimit = startTime + m_MaxTime;

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
                    IncrementIteration();

                    if (DateTimeOffset.Now.ToUnixTimeMilliseconds() > timeLimit)
                        loopState.Break();
                }
            );

            var (bestNode, bestMove) = rootNode.SelectBestNode();
            m_CachedNode = bestNode;
            m_CachedNode.Parent = null;
            SetWinPrediction(bestNode);
            var endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            PrintSearchData(rootNode, startTime, bestNode, bestMove, endTime);
            return bestMove;
        }

        private Move CalculateMoveSequential(Board board)
        {
            var rootNode = SetRootNode(board);
            var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var timeLimit = startTime + m_MaxTime;
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
                    IncrementIteration();
                }
            }

            var (bestNode, bestMove) = rootNode.SelectBestNode();
            m_CachedNode = bestNode;
            m_CachedNode.Parent = null;

            var endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            SetWinPrediction(bestNode);
            PrintSearchData(rootNode, startTime, bestNode, bestMove, endTime);
            return bestMove;
        }

        private Move CalculateMoveTesting(Board board)
        {
            //Rave test
            var rootNode = SetRootNode(board);
            var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var timeLimit = startTime + m_MaxTime;
            for (var iterations = 0;
                 (iterations < m_MaxIterations) & DateTimeOffset.Now.ToUnixTimeMilliseconds() < timeLimit;
                 iterations += BLOCK_SIZE)
            {
                for (var i = 0; i < BLOCK_SIZE; i++)
                {
                    var promisingNode = SelectRave(rootNode);
                    Expand(promisingNode);

                    var nodeToExplore = promisingNode;
                    if (promisingNode.Children.Count > 0)
                        nodeToExplore = promisingNode.GetRandomChildNode();
                    var (winningPlayer, whiteMoves, blackMoves) = SimulateRave(nodeToExplore);

                    BackPropagationRave(nodeToExplore, winningPlayer, whiteMoves, blackMoves);
                    IncrementIteration();
                }
            }

            var (bestNode, bestMove) = rootNode.SelectBestNode();
            m_CachedNode = bestNode;
            m_CachedNode.Parent = null;

            var endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            SetWinPrediction(bestNode);
            PrintSearchData(rootNode, startTime, bestNode, bestMove, endTime);
            return bestMove;
        }
        
        private Move CalculateGpu(Board board)
        {
            var rootNode = SetRootNode(board);
            var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var timeLimit = startTime + m_MaxTime;
            
            InitializeBuffers();
            for (var iterations = 0; (iterations < m_MaxIterations) & DateTimeOffset.Now.ToUnixTimeMilliseconds() < timeLimit; iterations += BLOCK_SIZE)
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
                    IncrementIteration();
                }
            }
            ReleaseBuffers();

            var (bestNode, bestMove) = rootNode.SelectBestNode();
            m_CachedNode = bestNode;
            m_CachedNode.Parent = null;

            var endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            SetWinPrediction(bestNode);
            PrintSearchData(rootNode, startTime, bestNode, bestMove, endTime);
            return bestMove;
        }

        private void IncrementIteration()
        {
            if (m_CurrentPlayer == Piece.BLACK)
                s_BlackIterationsRun++;
            else
                s_WhiteIterationsRun++;
        }
        
        private void IncrementSimulation(int value)
        {
            if (m_CurrentPlayer == Piece.BLACK)
                s_BlackSimulationsRun += value;
            else
                s_WhiteSimulationsRun += value;
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
                    IncrementIteration();
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

        private static Node SelectRave(Node node)
        {
            var currentNode = node;
            while (currentNode.Children.Count > 0)
                currentNode = SelectNodeWithRave(currentNode);
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

        private static void BackPropagationRave(Node nodeToExplore, int winningPlayer, List<Move> whiteMoves,
            List<Move> blackMoves)
        {
            var currentNode = nodeToExplore;
            while (currentNode.Parent != null)
            {
                var currentPlayer = currentNode.Parent.Board.GetCurrentPlayer();
                
                double reward = (winningPlayer == currentPlayer) ? 1 : -1;
                if (winningPlayer == 0)
                    reward = 0;
                
                currentNode.NumVisits++;
                currentNode.NumWins += (winningPlayer == currentPlayer) ? 1 : 0;
                currentNode.Score += reward;

                // Rave updates
                if (currentNode.Parent != null)
                {
                    var movesForCurrentPlayer = (currentPlayer == Player.WHITE) ? whiteMoves : blackMoves;
                    foreach (var childNode in currentNode.Parent.Children)
                    {
                        var childMove = currentNode.Parent.Board.GetAllPieces() ^ childNode.Board.GetAllPieces();
                        foreach (var move in movesForCurrentPlayer)
                        {
                            if (childMove != Board.IndexToBit(move.Index)) 
                                continue;
                            childNode.RaveScore += reward;
                            childNode.RaveVisits++;
                        }
                    }
                }
                currentNode = currentNode.Parent;
            }
            currentNode.NumVisits++;
        }

        private int Simulate(Node node)
        {
            var tempNode = node.Copy();
            var winningPlayer = tempNode.Board.GetBoardState();
            while (winningPlayer == IS_RUNNING)
            {
                tempNode.RandomMove();
                m_NodesVisited++;
                winningPlayer = tempNode.Board.GetBoardState();
            }
            IncrementSimulation(1);
            return winningPlayer;
        }
        
        private int SimulateGpu(Node node)
        {
            var blackPieces = node.Board.GetPiecesBitBoard(Player.BLACK);
            var whitePieces = node.Board.GetPiecesBitBoard(Player.WHITE);
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
            
            IncrementSimulation(NUM_GPU_SIMS);
            return winningPlayer;
        }

        private (int, List<Move> whiteMoves, List<Move> blackMoves) SimulateRave(Node node)
        {
            var whiteMoves = new List<Move>();
            var blackMoves = new List<Move>();
            var tempNode = node.Copy();
            var winningPlayer = tempNode.Board.GetBoardState();
            while (winningPlayer == IS_RUNNING)
            {
                var (player, move) = tempNode.RandomMoveRave();
                if (player == Player.BLACK)
                    blackMoves.Add(move);
                else
                    whiteMoves.Add(move);
                m_NodesVisited++;
                winningPlayer = tempNode.Board.GetBoardState();
            }

            return (winningPlayer, whiteMoves, blackMoves);
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

        private static Node SelectNodeWithRave(Node node)
        {
            var selectedNode = node.Children[0];
            for (var i = 1; i < node.Children.Count; i++)
            {
                var currentNode = node.Children[i];
                if (currentNode.CalculateUctRave() > selectedNode.CalculateUctRave())
                    selectedNode = currentNode;
            }

            return selectedNode;
        }

        private void PrintSearchData(Node rootNode, long startTime, Node bestNode, Move bestMove, long endTime)
        {
            Console.Log("Tree size: " + bestNode.NumVisits);
            Console.Log(rootNode.Board.GetCurrentPlayerAsString() + " plays " + bestMove);
            Console.Log("Search time: " + (endTime - startTime) + " ms");
            Console.Log("Iterations: " + (m_CurrentPlayer == Piece.BLACK ? s_BlackIterationsRun : s_WhiteIterationsRun));
            Console.Log("Nodes visited: " + m_NodesVisited);
            Console.Log("Win prediction: " + (bestNode.NumWins / bestNode.NumVisits * 100).ToString("0.##") + " %");
            Console.Log("----------------------------------------------------");
        }

        private void SetWinPrediction(Node bestNode)
        {
            if (m_CurrentPlayer == Piece.BLACK)
                s_BlackWinPrediction = bestNode.NumWins / bestNode.NumVisits * 100;
            else
                s_WhiteWinPrediction = bestNode.NumWins / bestNode.NumVisits * 100;
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