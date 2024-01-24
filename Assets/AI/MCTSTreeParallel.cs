using System;
using System.Threading.Tasks;
using Othello.Core;
using Console = Othello.Core.Console;

namespace Othello.AI
{
    public class MCTSTreeParallel : ISearchEngine
    {
        private Node m_cachedNode;
        private const int m_IsRunning = -1;
        private const int m_blockSize = 100;
        private readonly int m_maxTime;
        private readonly int m_maxIterations;
        private int m_nodesVisited;
        private int m_iterationsRun;
        private int m_numTrees;

        public MCTSTreeParallel(int maxIterations, int maxTime)
        {
            m_maxTime = maxTime;
            m_maxIterations = maxIterations;
        }

        public Move StartSearch(Board board)
        {
            m_nodesVisited = 0;
            m_iterationsRun = 0;
            var rootNode = SetRootNode(board);
            if (rootNode.Children.Count == 0)
                Expand(rootNode);
            m_numTrees = rootNode.Children.Count;
            return CalculateMove(rootNode);
        }

        private Move CalculateMove(Node rootNode)
        {
            var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var timeLimit = startTime + m_maxTime;

            Task[] tasks = new Task[rootNode.Children.Count];
            for (int i = 0; i < rootNode.Children.Count; i++)
            {
                var treeNode = rootNode.Children[i];
                Expand(treeNode);
                tasks[i] = Task.Factory.StartNew(() => { RunTree(treeNode, timeLimit); });
            }
            Task.WaitAll(tasks);

            var bestNode = rootNode.SelectBestNode();
            m_cachedNode = bestNode;
            m_cachedNode.Parent = null;
            var endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            Console.Log("Tree size: " + bestNode.NumVisits);
            Console.Log(rootNode.Board.GetCurrentPlayerAsString() + " plays " + bestNode.Board.GetLastMove().ToString());
            Console.Log("Search time: " + (endTime - startTime) + " ms");
            Console.Log("Iterations: " + m_iterationsRun);
            Console.Log("Nodes visited: " + m_nodesVisited);
            Console.Log("Win prediction: " + (bestNode.NumWins / bestNode.NumVisits * 100).ToString("0.##") + " %");
            Console.Log("----------------------------------------------------");
            return bestNode.Board.GetLastMove();
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
                    m_iterationsRun++;
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

    }

    struct ThreadState
    {
        Node node;
        long timeLimit;

        public ThreadState(Node node, long timeLimit)
        {
            this.node = node;
            this.timeLimit = timeLimit;
        }
    }

}