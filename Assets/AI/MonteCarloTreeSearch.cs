using System;
using System.Linq;

using Othello.Core;

namespace Othello.AI
{
    public class MonteCarloTreeSearch : ISearchEngine
    {
        private int m_player;
        private Tree m_cachedTree;
        private readonly int m_iterations;
        private const int m_IsRunning = -1;
        
        public MonteCarloTreeSearch(int iterations)
        {
            m_iterations = iterations;
        }

        public Move StartSearch(Board board)
        {
            return CalculateMove(board);
        }
        
        private Move CalculateMove(Board board)
        {
            m_player = board.GetCurrentPlayer();
            
            var tree = new Tree();
            SetCachedTreeIfPossible(board, tree);
            
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

            var bestNode = rootNode.GetChildWithHighestScore();
            m_cachedTree = new Tree() { Root = bestNode };
            return bestNode.State.Board.GetLastMove();
        }

        private void SetCachedTreeIfPossible(Board board, Tree tree)
        {
            if (m_cachedTree != null) // Only checking 1 depth in cachedTree atm
            {
                foreach (var node in m_cachedTree.Root.ChildArray)
                    if (node.State.Board.Equals(board))
                        tree.Root = node;
            }
            tree.Root.State.Board ??= board.Copy();
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