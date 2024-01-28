using System;
using System.Threading.Tasks;
using Othello.Core;
using Console = Othello.Core.Console;

namespace Othello.AI
{
    public class MCTSRootParallel : ISearchEngine
    {
        private static object m_lock = new object();
        private Node m_cachedNode;
        private const int m_IsRunning = -1;
        private readonly int m_maxTime;
        private readonly int m_maxIterations;
        private volatile int m_nodesVisited;
        private volatile int m_iterationsRun;

        public MCTSRootParallel(int maxIterations, int maxTime)
        {
            m_maxTime = maxTime;
            m_maxIterations = maxIterations;
        }

        public Move StartSearch(Board board)
        {
            m_nodesVisited = 0;
            m_iterationsRun = 0;
            return CalculateMove(board);
        }

        private Move CalculateMove(Board board)
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
                        promisingNode = Selection(rootNode);
                        Expansion(promisingNode);
                    }

                    var nodeToExplore = promisingNode;
                    if (promisingNode.Children.Count > 0)
                        nodeToExplore = promisingNode.GetRandomChildNode();
                    var winningPlayer = Simulation(nodeToExplore);

                    BackPropagation(nodeToExplore, winningPlayer);
                    m_iterationsRun++;

                    if (DateTimeOffset.Now.ToUnixTimeMilliseconds() > timeLimit)
                        loopState.Break();
                }
            );

            var bestNode = rootNode.SelectBestNode();
            m_cachedNode = bestNode;
            m_cachedNode.Parent = null;
            var endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            Console.Log("Tree size: " + bestNode.NumVisits);
            Console.Log(board.GetCurrentPlayerAsString() + " plays " + bestNode.Board.GetLastMove().ToString());
            Console.Log("Search time: " + (endTime - startTime) + " ms");
            Console.Log("Iterations: " + m_iterationsRun);
            Console.Log("Nodes visited: " + m_nodesVisited);
            Console.Log("Win prediction: " + (bestNode.NumWins / bestNode.NumVisits * 100).ToString("0.##") + " %");
            Console.Log("----------------------------------------------------");
            return bestNode.Board.GetLastMove();
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

        private static Node Selection(Node node)
        {
            var currentNode = node;
            while (currentNode.Children.Count > 0)
                currentNode = SelectNodeWithUct(currentNode);
            return currentNode;
        }

        private static void Expansion(Node node)
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

        private int Simulation(Node node)
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

}