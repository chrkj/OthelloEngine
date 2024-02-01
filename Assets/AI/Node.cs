using System;
using System.Collections.Generic;

using Othello.Core;

namespace Othello.AI
{
    public class Node
    {
        public Node Parent;
        public double Score;
        public double NumVisits;
        public readonly Board Board;
        public readonly List<Node> Children = new List<Node>();
        public volatile int NumWins = 0;
        
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

        public Node SelectBestNode()
        {
            var bestNode = Children[0];
            for(var i = 1; i < Children.Count; ++i) 
            {
                if (CalculateScore(Children[i]) > CalculateScore(bestNode))
                    bestNode = Children[i];
            }
            return bestNode;
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
                newBoard.ChangePlayer();
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
                Board.ChangePlayer();
                return;
            }
            var randomMove = legalMoves[new Random().Next(legalMoves.Length)];
            Board.MakeMove(randomMove);
            Board.ChangePlayer();
        }
        
        public double CalculateUct()
        {
            if (NumVisits == 0) 
                return int.MaxValue;
            return (Score / NumVisits) + Math.Sqrt(2.0) * Math.Sqrt(Math.Log(Parent.NumVisits) / NumVisits);
        }
    }
}