using System;
using System.Threading;
using System.Collections.Generic;

using Othello.Core;

namespace Othello.AI
{
    public class MctsThreading : ISearchEngine
    {
        private int m_player;
        private Board m_board;
        private Node m_cachedNode;
        private const int m_IsRunning = -1;
        private readonly int m_iterations;
        private Node mergedNode;
        
        public MctsThreading(int iterations)
        {
            m_iterations = iterations;
        }

        public Move StartSearch(Board board)
        {
            m_board = board;
            m_player = board.GetCurrentPlayer();
            
            var threads = new List<Thread>();
            var processorCount = Environment.ProcessorCount;
            for (var i = 0; i < processorCount; i++)
            {
                var thread = new Thread(CalculateMove);
                thread.Start();
                threads.Add(thread);
            }
            foreach(var thread in threads) thread.Join();
        
            var bestNode = mergedNode.SelectBestNode();
            m_cachedNode = bestNode;
            return bestNode.Board.GetLastMove();
        }
        
        private void CalculateMove()
        {
            var rootNode = new Node(m_board.Copy());
            for (var i = 0; i < m_iterations; i++)
            {
                var promisingNode = Selection(rootNode);
                
                Expansion(promisingNode); // Check for terminalstate?

                var nodeToExplore = promisingNode;
                if (promisingNode.Children.Count > 0) nodeToExplore = promisingNode.GetRandomChildNode();
                
                var winningPlayer = Simulation(nodeToExplore);
                BackPropagation(nodeToExplore, winningPlayer);
            }
            MergeTrees(rootNode);
        }

        private void MergeTrees(Node node)
        {
            lock (this)
            {
                mergedNode ??= node;
                foreach (var newNode in node.Children)
                {
                    foreach (var mergeNode in mergedNode.Children)
                    {
                        if (newNode.Board.GetLastMove() != mergeNode.Board.GetLastMove()) continue;
                        mergedNode.NumWins += node.NumWins;
                        mergedNode.NumVisits += node.NumVisits;
                    }
                }
            }
        }

        private Node SetRootNode()
        {
            Node rootNode = null;
            if (m_cachedNode != null)
            {
                foreach (var node in m_cachedNode.Children)
                    if (node.Board.Equals(m_board))
                        rootNode = node;
            }
            rootNode ??= new Node(m_board.Copy());
            return rootNode;
        }

        private static Node Selection(Node node) 
        {
            var currentNode = node;
            while (currentNode.Children.Count > 0) currentNode = SelectNodeWithUct(currentNode);
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
            var simulationScore = m_player == winningPlayer ? 1 : 0;
            while (currentNode != null) 
            {
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

        private static Node SelectNodeWithUct(Node node)
        {
            var selectedNode = node.Children[0];
            for (int i = 1; i < node.Children.Count; i++)
            {
                var currentNode = node.Children[i];
                if (currentNode.CalculateUct() > selectedNode.CalculateUct()) selectedNode = currentNode;
            }
            return selectedNode;
        }

    }
    
}