using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Othello.Core;
using static Othello.Core.Settings;
using Console = Othello.Core.Console;

namespace Othello.AI
{
    public class MCTS : ISearchEngine
    {
        private static readonly object m_lock = new object();
        private Node m_cachedNode;
        private const int m_IsRunning = -1;
        private const int m_blockSize = 50;
        private readonly int m_maxTime;
        private readonly int m_maxIterations;
        private int m_nodesVisited;
        private int m_CurrentPlayer;
        private readonly MctsType m_MctsType;
        private int m_numTrees;

        public static int s_WhiteIterationsRun;
        public static int s_BlackIterationsRun;
        public static double s_WhiteWinPrediction;
        public static double s_BlackWinPrediction;

        public MCTS(int maxIterations, int maxTime, MctsType mctsType)
        {
            m_maxTime = maxTime;
            m_maxIterations = maxIterations;
            s_WhiteIterationsRun = 0;
            s_BlackIterationsRun = 0;
            s_WhiteWinPrediction = 0;
            s_BlackWinPrediction = 0;
            m_MctsType = mctsType;
        }

        public Move StartSearch(Board board)
        {
            m_nodesVisited = 0;
            m_CurrentPlayer = board.GetCurrentPlayer() == Piece.Black ? Piece.Black : Piece.White;
            if (m_CurrentPlayer == Piece.Black)
                s_BlackIterationsRun = 0;
            else
                s_WhiteIterationsRun = 0;

            switch (m_MctsType)
            {
                case MctsType.Sequential:
                    return CalculateMoveSequential(board);
                case MctsType.RootParallel:
                    return CalculateMoveRoot(board);
                case MctsType.TreeParallel:
                    return CalculateMoveTree(board);
                case MctsType.Testing:
                    return CalculateMoveTesting(board);
                default:
                    throw new NotImplementedException();
            }
        }

        private Move CalculateMoveTree(Board board)
        {
            var rootNode = SetRootNode(board);
            if (rootNode.Children.Count == 0)
                Expand(rootNode);
            m_numTrees = rootNode.Children.Count;

            var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var timeLimit = startTime + m_maxTime;

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

            var bestNode = rootNode.SelectBestNode();
            m_cachedNode = bestNode;
            m_cachedNode.Parent = null;
            var endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            SetWinPrediction(bestNode);
            PrintSearchData(rootNode, startTime, bestNode, endTime);
            return bestNode.Board.GetLastMove();
        }

        private Move CalculateMoveRoot(Board board)
        {
            var rootNode = SetRootNode(board);
            var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var timeLimit = startTime + m_maxTime;

            var thread = Parallel.For(0, m_maxIterations + 1,
            (i, loopState) =>
            {
                Node promisingNode;
                lock (m_lock)
                {
                    promisingNode = Select(rootNode);
                    Expand(promisingNode);
                }

                var nodeToExplore = promisingNode;
                if (promisingNode.Children.Count > 0)
                    nodeToExplore = promisingNode.GetRandomChildNode();
                var winningPlayer = Simulate(nodeToExplore);

                BackPropagation(nodeToExplore, winningPlayer);
                IncrementIterarion();

                if (DateTimeOffset.Now.ToUnixTimeMilliseconds() > timeLimit)
                    loopState.Break();
            }
            );

            var bestNode = rootNode.SelectBestNode();
            m_cachedNode = bestNode;
            m_cachedNode.Parent = null;
            SetWinPrediction(bestNode);
            var endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            PrintSearchData(rootNode, startTime, bestNode, endTime);
            return bestNode.Board.GetLastMove();
        }

        private Move CalculateMoveSequential(Board board)
        {
            var rootNode = SetRootNode(board);
            var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var timeLimit = startTime + m_maxTime;
            for (var iterations = 0; (iterations < m_maxIterations) & DateTimeOffset.Now.ToUnixTimeMilliseconds() < timeLimit; iterations += m_blockSize)
            {
                for (var i = 0; i < m_blockSize; i++)
                {
                    var promisingNode = Select(rootNode);
                    Expand(promisingNode);

                    var nodeToExplore = promisingNode;
                    if (promisingNode.Children.Count > 0)
                        nodeToExplore = promisingNode.GetRandomChildNode();
                    var winningPlayer = Simulate(nodeToExplore);

                    BackPropagation(nodeToExplore, winningPlayer);
                    IncrementIterarion();
                }
            }
            var bestNode = rootNode.SelectBestNode();
            m_cachedNode = bestNode;
            m_cachedNode.Parent = null;

            var endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            SetWinPrediction(bestNode);
            PrintSearchData(rootNode, startTime, bestNode, endTime);
            return bestNode.Board.GetLastMove();
        }

        private Move CalculateMoveTesting(Board board)
        {
            var rootNode = SetRootNode(board);
            var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var timeLimit = startTime + m_maxTime;
            for (var iterations = 0; (iterations < m_maxIterations) & DateTimeOffset.Now.ToUnixTimeMilliseconds() < timeLimit; iterations += m_blockSize)
            {
                for (var i = 0; i < m_blockSize; i++)
                {
                    var promisingNode = Select(rootNode);
                    Expand(promisingNode);

                    var nodeToExplore = promisingNode;
                    if (promisingNode.Children.Count > 0)
                        nodeToExplore = promisingNode.GetRandomChildNode();
                    var winningPlayer = Simulate(nodeToExplore);

                    BackPropagation(nodeToExplore, winningPlayer);
                    IncrementIterarion();
                }
            }
            var bestNode = rootNode.SelectBestNode();
            m_cachedNode = bestNode;
            m_cachedNode.Parent = null;

            var endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            SetWinPrediction(bestNode);
            PrintSearchData(rootNode, startTime, bestNode, endTime);
            return bestNode.Board.GetLastMove();
        }

        private void IncrementIterarion()
        {
            if (m_CurrentPlayer == Piece.Black)
                s_BlackIterationsRun++;
            else
                s_WhiteIterationsRun++;
        }

        private void RunTree(Node rootNode, long timeLimit)
        {
            for (var iterations = 0; (iterations < m_maxIterations / m_numTrees) & DateTimeOffset.Now.ToUnixTimeMilliseconds() < timeLimit; iterations += m_blockSize)
            {
                for (var i = 0; i < m_blockSize; i++)
                {
                    var promisingNode = Select(rootNode);
                    Expand(promisingNode);

                    var nodeToExplore = promisingNode;
                    if (promisingNode.Children.Count > 0)
                        nodeToExplore = promisingNode.GetRandomChildNode();
                    var winningPlayer = Simulate(nodeToExplore);

                    BackPropagation(nodeToExplore, winningPlayer);
                    IncrementIterarion();
                }
            }
        }

        private Node SetRootNode(Board board)
        {
            Node rootNode = null;
            if (m_cachedNode != null)
            {
                foreach (var node in m_cachedNode.Children)
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
            while (winningPlayer == m_IsRunning)
            {
                tempNode.RandomMove();
                m_nodesVisited++;
                winningPlayer = tempNode.Board.GetBoardState();
            }
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

        private void PrintSearchData(Node rootNode, long startTime, Node bestNode, long endTime)
        {
            Console.Log("Tree size: " + bestNode.NumVisits);
            Console.Log(rootNode.Board.GetCurrentPlayerAsString() + " plays " + bestNode.Board.GetLastMove().ToString());
            Console.Log("Search time: " + (endTime - startTime) + " ms");
            Console.Log("Iterations: " + (m_CurrentPlayer == Piece.Black ? s_BlackIterationsRun : s_WhiteIterationsRun));
            Console.Log("Nodes visited: " + m_nodesVisited);
            Console.Log("Win prediction: " + (bestNode.NumWins / bestNode.NumVisits * 100).ToString("0.##") + " %");
            Console.Log("----------------------------------------------------");
        }

        private void SetWinPrediction(Node bestNode)
        {
            if (m_CurrentPlayer == Piece.Black)
                s_BlackWinPrediction = bestNode.NumWins / bestNode.NumVisits * 100;
            else
                s_WhiteWinPrediction = bestNode.NumWins / bestNode.NumVisits * 100;
        }

    }

}