using System;
using System.Threading;
using System.Threading.Tasks;

using Othello.UI;
using Othello.Core;
using Console = Othello.Core.Console;

namespace Othello.AI
{
    public class Mcts : ISearchEngine
    {
        public static int s_WhiteIterationsRun;
        public static int s_BlackIterationsRun;
        public static double s_WhiteWinPrediction;
        public static double s_BlackWinPrediction;
        
        private const int BLOCK_SIZE = 50;
        private const int IS_RUNNING = -1;
        private Node m_CachedNode;
        private int m_NodesVisited;
        private int m_CurrentPlayer;
        private int m_NumTrees;
        private readonly int m_MaxTime;
        private readonly int m_MaxIterations;
        private readonly MenuUI.MctsType m_MctsType;

        public Mcts(int maxIterations, int maxTime, MenuUI.MctsType mctsType)
        {
            m_MaxTime = maxTime;
            m_MaxIterations = maxIterations;
            s_WhiteIterationsRun = 0;
            s_BlackIterationsRun = 0;
            s_WhiteWinPrediction = 0;
            s_BlackWinPrediction = 0;
            m_MctsType = mctsType;
        }

        public Move StartSearch(Board board)
        {
            m_NodesVisited = 0;
            m_CurrentPlayer = board.GetCurrentPlayer() == Piece.BLACK ? Piece.BLACK : Piece.WHITE;
            if (m_CurrentPlayer == Piece.BLACK)
                s_BlackIterationsRun = 0;
            else
                s_WhiteIterationsRun = 0;

            return m_MctsType switch
            {
                MenuUI.MctsType.Sequential => CalculateMoveSequential(board),
                MenuUI.MctsType.RootParallel => CalculateMoveRoot(board),
                MenuUI.MctsType.TreeParallel => CalculateMoveTree(board),
                MenuUI.MctsType.Testing => CalculateMoveTesting(board),
                _ => throw new NotImplementedException()
            };
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

            var bestNode = rootNode.SelectBestNode();
            m_CachedNode = bestNode;
            m_CachedNode.Parent = null;
            var endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            SetWinPrediction(bestNode);
            PrintSearchData(rootNode, startTime, bestNode, endTime);
            return bestNode.Board.GetLastMove();
        }

        private Move CalculateMoveRoot(Board board)
        {
            var rootNode = SetRootNode(board);
            var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var timeLimit = startTime + m_MaxTime;

            var _ = Parallel.For(0, m_MaxIterations + 1,
            (i, loopState) =>
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

            var bestNode = rootNode.SelectBestNode();
            m_CachedNode = bestNode;
            m_CachedNode.Parent = null;
            SetWinPrediction(bestNode);
            var endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            PrintSearchData(rootNode, startTime, bestNode, endTime);
            return bestNode.Board.GetLastMove();
        }

        private Move CalculateMoveSequential(Board board)
        {
            var rootNode = SetRootNode(board);
            var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var timeLimit = startTime + m_MaxTime;
            for (var iterations = 0; (iterations < m_MaxIterations) & DateTimeOffset.Now.ToUnixTimeMilliseconds() < timeLimit; iterations += BLOCK_SIZE)
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
            var bestNode = rootNode.SelectBestNode();
            m_CachedNode = bestNode;
            m_CachedNode.Parent = null;

            var endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            SetWinPrediction(bestNode);
            PrintSearchData(rootNode, startTime, bestNode, endTime);
            return bestNode.Board.GetLastMove();
        }

        private Move CalculateMoveTesting(Board board)
        {
            var rootNode = SetRootNode(board);
            var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var timeLimit = startTime + m_MaxTime;
            for (var iterations = 0; (iterations < m_MaxIterations) & DateTimeOffset.Now.ToUnixTimeMilliseconds() < timeLimit; iterations += BLOCK_SIZE)
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
            var bestNode = rootNode.SelectBestNode();
            m_CachedNode = bestNode;
            m_CachedNode.Parent = null;

            var endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            SetWinPrediction(bestNode);
            PrintSearchData(rootNode, startTime, bestNode, endTime);
            return bestNode.Board.GetLastMove();
        }

        private void IncrementIteration()
        {
            if (m_CurrentPlayer == Piece.BLACK)
                s_BlackIterationsRun++;
            else
                s_WhiteIterationsRun++;
        }

        private void RunTree(Node rootNode, long timeLimit)
        {
            for (var iterations = 0; (iterations < m_MaxIterations / m_NumTrees) & DateTimeOffset.Now.ToUnixTimeMilliseconds() < timeLimit; iterations += BLOCK_SIZE)
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
                m_NodesVisited++;
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
            Console.Log(rootNode.Board.GetCurrentPlayerAsString() + " plays " + bestNode.Board.GetLastMove());
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

    }

}