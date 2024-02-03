using System;
using System.Collections.Generic;

using Othello.Core;

namespace Othello.AI
{
    public class Node
    {
        public Node Parent;
        public int NumWins;
        public double Score;
        public double NumVisits;
        public readonly Board Board;
        public readonly List<Node> Children = new();
        
        public Node(Board board)
        {
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
            return Children[new Random().Next(Children.Count)];
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
            return node.NumVisits;
        }
        
        public List<Node> CreateChildNodes()
        {
            var notes = new List<Node>();
            foreach (var legalMove in Board.GenerateLegalMoves())
            {
                var newBoard = Board.Copy();
                newBoard.MakeMove(legalMove);
                newBoard.ChangePlayerToMove();
                var newNode = new Node(newBoard);
                notes.Add(newNode);
            }
            return notes;
        }
            
        public void RandomMove() 
        {
            var legalMoves = Board.GenerateLegalMoves();
            if (legalMoves.Length == 0)
            {
                Board.ChangePlayerToMove();
                return;
            }
            var randomMove = legalMoves[new Random().Next(legalMoves.Length)];
            Board.MakeMove(randomMove);
            Board.ChangePlayerToMove();
        }
        
        public double CalculateUct()
        {
            if (NumVisits == 0) 
                return int.MaxValue;
            return (Score / NumVisits) + Math.Sqrt(2.0) * Math.Sqrt(Math.Log(Parent.NumVisits) / NumVisits);
        }
    }
}