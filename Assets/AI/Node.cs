using System;
using System.Collections.Generic;

using Othello.Core;

namespace Othello.AI
{
    public class Node
    {
        public int NumWins;
        public int NumVisits;
        public int Score;
        public Node Parent;
        public readonly Board Board;
        public readonly List<Node> Children = new();
        
        private readonly Random m_Random;
        
        public Node(Board board)
        {
            m_Random = new Random();  
            Board = board;
        }

        public Node Copy() 
        {
            var copyNode = new Node(Board.Copy())
            {
                NumVisits = NumVisits,
                Score = Score
            };
            return copyNode;
        }

        public Node GetRandomChildNode()
        {
            return Children[m_Random.Next(Children.Count)];
        }

        private Move GetBestMove(Node bestNode)
        {
            Move bestMove = Move.NULLMOVE;
            var currentBoard = Board.GetAllPieces();
            var previousBoard = bestNode.Board.GetAllPieces();

            var bestMoveBitboard = (currentBoard ^ previousBoard);
            for (int i = 0; i < 64; i++)
            {
                ulong mask = (ulong)1 << i;
                if ((bestMoveBitboard & mask) != 0)
                    bestMove = new Move(i);
            }
            return bestMove;
        }

        private static double CalculateScore(Node node)
        {
            if (node.NumVisits == 0)
                return -1;
            return (double)node.NumWins / node.NumVisits;
        }
        
        public List<Node> CreateChildNodes()
        {
            var notes = new List<Node>();
            Span<Move> legalMoves = stackalloc Move[Board.MAX_LEGAL_MOVES];
            Board.GenerateLegalMoves(ref legalMoves);
            foreach (var legalMove in legalMoves)
            {
                var newBoard = Board.Copy();
                newBoard.MakeMove(legalMove);
                newBoard.ChangePlayer();
                var newNode = new Node(newBoard);
                notes.Add(newNode);
            }
            return notes;
        }
            
        public void RandomMove(RolloutPolicy policy)
        {
            Span<Move> legalMoves = stackalloc Move[Board.MAX_LEGAL_MOVES];
            Board.GenerateLegalMoves(ref legalMoves);
            if (legalMoves.Length == 0)
            {
                Board.ChangePlayer();
                return;
            }

            var move = policy.Pick(legalMoves, m_Random);
            Board.MakeMove(move);
            Board.ChangePlayer();
        }
        
        public double CalculateUct()
        {
            if (NumVisits == 0)
                return int.MaxValue;
            return (Score / (2.0 * NumVisits)) + Math.Sqrt(2.0) * Math.Sqrt(Math.Log(Parent.NumVisits) / NumVisits);
        }
        
        public (Node, Move) SelectBestNode()
        {
            var bestNode = Children[0];
            for(var i = 1; i < Children.Count; ++i) 
                if (CalculateScore(Children[i]) > CalculateScore(bestNode))
                    bestNode = Children[i];
            var bestMove = GetBestMove(bestNode);
            return (bestNode, bestMove);
        }
    }
}