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
        private int m_opponent;
        private readonly int m_iterations;
        
        private const int WinningScore = 10;

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
            m_opponent = board.GetCurrentOpponent();
            var tree = new Tree();
            var rootNode = tree.root;
            rootNode.state.board = new Board(board);

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

            var bestNode = rootNode.GetChildWithMaxScore();
            return bestNode.state.board.GetLastMove();
        }
        
        private static Node Selection(Node rootNode) 
        {
            var node = rootNode;
            while (node.childArray.Count != 0)
                node = UCT.FindBestNodeWithUCT(node);
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
        
        private void BackPropogation(Node nodeToExplore, int playerNo) 
        {
            var tempNode = nodeToExplore;
            while (tempNode != null) 
            {
                tempNode.state.visitCount++;
                if (m_player == playerNo) 
                    tempNode.state.winScore += WinningScore;
                tempNode = tempNode.parent;
            }
        }
        
        private int Simulation(Node node) 
        {
            Node tempNode = new Node(node);
            State tempState = tempNode.state;
            int boardStatus = tempState.board.CheckStatus();
            if (boardStatus == m_opponent) 
            {
                tempNode.parent.state.winScore = int.MinValue;
                return boardStatus;
            }
            while (boardStatus == -1) 
            {
                tempState.RandomPlay();
                boardStatus = tempState.board.CheckStatus();
            }
            return boardStatus;
        }
        
        private static class UCT 
        {
            private static double uctValue(int totalVisit, double nodeWinScore, int nodeVisit) {
                if (nodeVisit == 0) return int.MaxValue;
                return (nodeWinScore / nodeVisit) + 1.41 * Math.Sqrt(Math.Log(totalVisit) / nodeVisit);
            }
            
            public static Node FindBestNodeWithUCT(Node node) 
            {
                int parentVisit = node.state.visitCount;
                return node.childArray.OrderByDescending(c => uctValue(parentVisit, c.state.winScore, c.state.visitCount)).First();
            }
        }
            
        private class Node 
        {
            public State state = new State();
            public Node parent;
            public readonly List<Node> childArray = new List<Node>(); 
            private readonly Random random = new Random();

            public Node()
            {
            }
            
            public Node(Node node)
            {
                this.state = new State()
                {
                    board = new Board(node.state.board), visitCount = node.state.visitCount, winScore = node.state.visitCount
                };
                this.parent = node.parent;
                this.childArray.AddRange(node.childArray);
            }

            public Node GetRandomChildNode()
            {
                return childArray[random.Next(childArray.Count)];
            }

            public Node GetChildWithMaxScore()
            {
                return childArray.OrderByDescending(c => c.state.winScore).First();
            }
        }
        
        private class Tree
        {
            public readonly Node root;

            public Tree()
            {
                root = new Node();
            }
        }
        
        private class State 
        {
            public Board board;
            public int visitCount;
            public double winScore;
            private readonly Random rand = new Random();

            public List<State> GetAllPossibleStates()
            {
                var states = new List<State>();
                foreach (var legalMove in MoveGenerator.GenerateLegalMoves(board))
                {
                    var newBoard = new Board(board);
                    newBoard.MakeMove(new Move(legalMove.Key, board.GetCurrentPlayer(), legalMove.Value));
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
                var randomMove = legalMoves.ElementAt(rand.Next(0, legalMoves.Count));
                board.MakeMove(new Move(randomMove.Key, board.GetCurrentPlayer(), randomMove.Value));
                board.ChangePlayer();
            }
            
        }
        
    }
    
}