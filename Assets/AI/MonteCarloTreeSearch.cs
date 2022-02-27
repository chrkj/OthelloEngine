using System;
using Othello.Core;
using UnityEngine;

namespace Othello.AI
{
    public class MonteCarloTreeSearch : ISearchEngine
    {
        private Node m_cachedNode;
        private const int m_IsRunning = -1;
        private const int m_maxTime = 4000;
        private readonly int m_maxIterations;
        private int m_blockSize = 50;

        public MonteCarloTreeSearch(int maxIterations)
        {
            m_maxIterations = maxIterations;
        }

        public Move StartSearch(Board board)
        {
            return CalculateMove(board);
        }
        
        private Move CalculateMove(Board board)
        {
            var rootNode = SetRootNode(board);
            var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var timeLimit = startTime + m_maxTime;
            for (var iterations = 0; (iterations < m_maxIterations) & (DateTimeOffset.Now.ToUnixTimeMilliseconds()) < timeLimit; iterations += m_blockSize)
            {
                for (int i = 0; i < m_blockSize; i++)
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
                
                    // BaclPropagation
                    BackPropagation(nodeToExplore, winningPlayer);
                }
            }
            var bestNode = rootNode.SelectBestNode();
            m_cachedNode = bestNode;
            return bestNode.Board.GetLastMove();
            }

        private Node SetRootNode(Board board)
        {
            Node rootNode = null;
            if (m_cachedNode != null)
            {
                foreach (var node in m_cachedNode.Children)
                    if (node.Board.Equals(board))
                        rootNode = node;
            }
            rootNode ??= new Node(board.Copy());
            return rootNode;
        }

        private Node Selection(Node node) 
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
        
        private void BackPropagation(Node nodeToExplore, int winningPlayer)
        {
            var currentNode = nodeToExplore;
            while (currentNode != null) 
            {
                //TODO: Cleanup
                double simulationScore = winningPlayer == currentNode.Board.GetCurrentPlayer() ? 0 : 1;
                if (winningPlayer == 0) simulationScore = 0.5;
                currentNode.NumVisits++;
                currentNode.NumWins += simulationScore;
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
                winningPlayer = tempNode.Board.GetBoardState();
            }
            return winningPlayer;
        }

        private Node SelectNodeWithUct(Node node)
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