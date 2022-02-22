using System;
using System.Linq;
using System.Collections.Generic;
using Othello.Core;
using Random = System.Random;

namespace Othello.AI
{
    public class MonteCarloTreeSearch : ISearchEngine
    {
        private int m_player;
        private int m_oppenent;
        private Tree cachedTree;
        private readonly int m_iterations;
        private const int IsRunning = -1;
        
        public MonteCarloTreeSearch(int iterations)
        {
            m_iterations = iterations;
        }

        public int StartSearch(Board board)
        {
            return CalculateMove(board);
        }
        
        private int CalculateMove(Board board)
        {
            m_player = board.GetCurrentPlayer();
            m_oppenent = board.GetCurrentOpponent();
            var tree = new Tree();
            if (cachedTree != null) // Only checking 1 depth in cachedTree atm
            {
                foreach (var node in cachedTree.root.childArray)
                    if (node.state.board.Equals(board)) tree.root = node;
            }
            else
            {
                tree.root.state.board = board.Copy();
            }
            var rootNode = tree.root;

            for (int i = 0; i < m_iterations; i++)
            {
                var promisingNode = Selection(rootNode);
                if (!promisingNode.state.board.IsTerminalBoardState(promisingNode.state.board)) 
                    Expansion(promisingNode);

                var nodeToExplore = promisingNode;
                if (promisingNode.childArray.Count > 0) 
                    nodeToExplore = promisingNode.GetRandomChildNode();
                
                int winningPlayer = Simulation(nodeToExplore);
                BackPropogation(nodeToExplore, winningPlayer);
            }

            var bestNode = rootNode.GetChildWithHighestScore();
            cachedTree = new Tree() { root = bestNode };
            return bestNode.state.board.GetLastMove();
        }
        
        private Node Selection(Node rootNode) 
        {
            var node = rootNode;
            while (node.childArray.Count != 0) node = SelectNodeWithUct(node);
            return node;
        }
        
        private static void Expansion(Node node) 
        {
            var possibleStates = node.state.GetAllPossibleStates();
            foreach (var state in possibleStates)
            {
                var newNode = new Node { state = state, parent = node };
                node.childArray.Add(newNode);
            }
        }
        
        private void BackPropogation(Node nodeToExplore, int winningPlayer)
        {
            var currentNode = nodeToExplore;
            int simulationScore;
            if (winningPlayer == 0) simulationScore = 0;
            else simulationScore = m_player == winningPlayer ? 1 : 0;
            while (currentNode != null) 
            {
                if (winningPlayer == m_player) currentNode.win++;
                if (winningPlayer == m_oppenent) currentNode.loss++;
                if (winningPlayer == 0) currentNode.draw++;
                currentNode.state.noVisites++;
                currentNode.state.noWins += simulationScore;
                currentNode = currentNode.parent;
            }
        }
        
        private static int Simulation(Node node) 
        {
            Node tempNode = new Node(node);
            State tempState = tempNode.state;
            int winningPlayer = tempState.board.GetBoardState();
            while (winningPlayer == IsRunning)
            {
                tempState.RandomPlay();
                winningPlayer = tempState.board.GetBoardState();
            }
            return winningPlayer;
        }
        
        
        private static double CalculateUct(int totalVisit, double noWins, int noVisits) 
        {
            if (noVisits == 0) return int.MaxValue;
            return (noWins / noVisits) + (Math.Sqrt(2) * Math.Sqrt(Math.Log(totalVisit) / noVisits));
        }
        
        public Node SelectNodeWithUct(Node node) 
        {
            return node.childArray.OrderByDescending(currentNode => CalculateUct(currentNode.parent.state.noVisites, currentNode.state.noWins, currentNode.state.noVisites)).First();
        }


        public class Node 
        {
            public State state = new State();
            public Node parent;
            public readonly List<Node> childArray = new List<Node>(); 
            private readonly Random random = new Random();
            public int win;
            public int loss;
            public int draw;

            public Node()
            {
            }
            
            public Node(Node node)
            {
                state = new State()
                {
                    board = node.state.board.Copy(), 
                    noVisites = node.state.noVisites, 
                    noWins = node.state.noVisites
                };
            }

            public Node GetRandomChildNode()
            {
                return childArray[random.Next(childArray.Count)];
            }

            public Node GetChildWithHighestScore()
            {
                return childArray.OrderByDescending(node => node.state.noWins / (node.state.noVisites == 0 ? Int32.MaxValue : node.state.noVisites)).First();
            }
        }
        
        private class Tree
        {
            public Node root;

            public Tree()
            {
                root = new Node();
            }
        }

        public class State 
        {
            public Board board;
            public int noVisites;
            public int noWins;

            public List<State> GetAllPossibleStates()
            {
                var states = new List<State>();
                foreach (var legalMove in MoveGenerator.GenerateLegalMoves(board))
                {
                    var newBoard = board.Copy();
                    newBoard.MakeMove(legalMove, MoveGenerator.GetCaptureIndices(legalMove, newBoard));
                    newBoard.ChangePlayer();
                    var newState = new State { board = newBoard };
                    states.Add(newState);
                }
                return states;
            }
            
            public void RandomPlay() 
            {
                var legalMoves = MoveGenerator.GenerateLegalMoves(board);
                if (legalMoves.Count == 0)
                {
                    board.ChangePlayer();
                    return;
                }
                var rand = new Random();
                var randomMove = legalMoves[rand.Next(legalMoves.Count)];
                var captures = MoveGenerator.GetCaptureIndices(randomMove, board);
                board.MakeMove(randomMove, captures);
                board.ChangePlayer();
            }
            
        }
        
    }
    
}