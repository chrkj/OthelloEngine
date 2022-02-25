using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Othello.Core;

namespace Othello.AI
{
    public class MctsThreading : ISearchEngine
    {
        private Tree m_tree;
        private int m_player;
        private Board m_board;
        private readonly int m_iterations;
        private const int m_IsRunning = -1;
        
        public MctsThreading(int iterations)
        {
            m_iterations = iterations;
        }

        public Move StartSearch(Board board)
        {
            m_board = board;
            m_tree = new Tree();
            m_tree.Root.State.Board = m_board.Copy();
            Expansion(m_tree.Root); 
            var threads = new List<Thread>();
            for (var i = 0; i < 8; i++)
            {
                var thread = new Thread(CalculateMove);
                thread.Start();
                threads.Add(thread);
            }
            foreach(var thread in threads)
            {
                thread.Join();
            }
            var bestNode = m_tree.Root.GetChildWithHighestScore();
            return bestNode.State.Board.GetLastMove();
        }

        private void MergeTrees(Tree tree)
        {
            lock (this)
            {
                m_tree.Root.ChildArray.Sort();
                tree.Root.ChildArray.Sort();
                for (var i = 0; i < tree.Root.ChildArray.Count; i++)
                {
                    m_tree.Root.ChildArray[i].State.NumWins += tree.Root.ChildArray[i].State.NumWins;
                    m_tree.Root.ChildArray[i].State.NumVisits += tree.Root.ChildArray[i].State.NumVisits;
                }
            }
        }

        private void CalculateMove()
        {
            m_player = m_board.GetCurrentPlayer();
            
            var tree = new Tree();
            tree.Root.State.Board = m_board.Copy();
            
            var rootNode = tree.Root;
            for (var i = 0; i < m_iterations; i++)
            {
                var promisingNode = Selection(rootNode);
                if (!promisingNode.State.Board.IsTerminalBoardState()) 
                    Expansion(promisingNode);

                var nodeToExplore = promisingNode;
                if (promisingNode.ChildArray.Count > 0) 
                    nodeToExplore = promisingNode.GetRandomChildNode();
                
                var winningPlayer = Simulation(nodeToExplore);
                BackPropagation(nodeToExplore, winningPlayer);
            }
            MergeTrees(tree);
        }

        private static Node Selection(Node rootNode) 
        {
            var node = rootNode;
            while (node.ChildArray.Count != 0) node = SelectNodeWithUct(node);
            return node;
        }
        
        private static void Expansion(Node node) 
        {
            var possibleStates = node.State.GetAllPossibleStates();
            foreach (var state in possibleStates)
            {
                var newNode = new Node { State = state, Parent = node };
                node.ChildArray.Add(newNode);
            }
        }
        
        private void BackPropagation(Node nodeToExplore, int winningPlayer)
        {
            var currentNode = nodeToExplore;
            var simulationScore = m_player == winningPlayer ? 1 : 0;
            while (currentNode != null) 
            {
                currentNode.State.NumVisits++;
                currentNode.State.NumWins += simulationScore;
                currentNode = currentNode.Parent;
            }
        }
        
        private static int Simulation(Node node) 
        {
            var tempNode = new Node(node);
            var tempState = tempNode.State;
            var winningPlayer = tempState.Board.GetBoardState();
            while (winningPlayer == m_IsRunning)
            {
                tempState.RandomPlay();
                winningPlayer = tempState.Board.GetBoardState();
            }
            return winningPlayer;
        }
        
        private static double CalculateUct(int totalVisit, double noWins, int noVisits) 
        {
            if (noVisits == 0) return int.MaxValue;
            return (noWins / noVisits) + (Math.Sqrt(2) * Math.Sqrt(Math.Log(totalVisit) / noVisits));
        }

        private static Node SelectNodeWithUct(Node node) 
        {
            return node.ChildArray.OrderByDescending(currentNode => CalculateUct(currentNode.Parent.State.NumVisits, currentNode.State.NumWins, currentNode.State.NumVisits)).First();
        }

    }
    
}