using System;
using Othello.Core;
using Console = Othello.Core.Console;

namespace Othello.AI
{
    public class MCTS : ISearchEngine
    {
        private Node m_cachedNode;
        private const int m_IsRunning = -1;
        private const int m_blockSize = 50;
        private readonly int m_maxTime;
        private readonly int m_maxIterations;
        private int m_nodesVisited;
        private int m_CurrentPlayer;
        
        public static int s_WhiteIterationsRun;
        public static int s_BlackIterationsRun;
        public static double s_WhiteWinPrediction;
        public static double s_BlackWinPrediction;

        public MCTS(int maxIterations, int maxTime)
        {
            m_maxTime = maxTime;
            m_maxIterations = maxIterations;
            s_WhiteIterationsRun = 0;
            s_BlackIterationsRun = 0;
            s_WhiteWinPrediction = 0;
            s_BlackWinPrediction = 0;
        }

        public Move StartSearch(Board board)
        {
            m_nodesVisited = 0;
            m_CurrentPlayer = board.GetCurrentPlayer() == Piece.Black ? Piece.Black : Piece.White;
            if (m_CurrentPlayer == Piece.Black)
                s_BlackIterationsRun = 0;
            else
                s_WhiteIterationsRun = 0;
            return CalculateMove(board);
        }

        private Move CalculateMove(Board board)
        {
            var rootNode = SetRootNode(board);
            var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var timeLimit = startTime + m_maxTime;
            for (var iterations = 0; (iterations < m_maxIterations) & DateTimeOffset.Now.ToUnixTimeMilliseconds() < timeLimit; iterations += m_blockSize)
            {
                for (var i = 0; i < m_blockSize; i++)
                {
                    // Selection
                    var promisingNode = Selection(rootNode);

                    // Expansion
                    Expansion(promisingNode);

                    // Simulation
                    var nodeToExplore = promisingNode;
                    if (promisingNode.Children.Count > 0)
                        nodeToExplore = promisingNode.GetRandomChildNode();
                    var winningPlayer = Simulation(nodeToExplore);

                    // BackPropagation
                    BackPropagation(nodeToExplore, winningPlayer);
                    if (m_CurrentPlayer == Piece.Black)
                        s_BlackIterationsRun++;
                    else
                        s_WhiteIterationsRun++;
                }
            }
            var bestNode = rootNode.SelectBestNode();
            m_cachedNode = bestNode;
            m_cachedNode.Parent = null;
            var endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            if (m_CurrentPlayer == Piece.Black)
                s_BlackWinPrediction = bestNode.NumWins / bestNode.NumVisits * 100;
            else
                s_WhiteWinPrediction = bestNode.NumWins / bestNode.NumVisits * 100;

            Console.Log("Tree size: " + bestNode.NumVisits);
            Console.Log(board.GetCurrentPlayerAsString() + " plays " + bestNode.Board.GetLastMove().ToString());
            Console.Log("Search time: " + (endTime - startTime) + " ms");
            Console.Log("Iterations: " + (m_CurrentPlayer == Piece.Black ? s_BlackIterationsRun : s_WhiteIterationsRun));
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