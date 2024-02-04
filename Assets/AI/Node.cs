using System;
using System.Collections.Generic;

using Othello.Core;

namespace Othello.AI
{
    public class Node
    {
        public Node Parent;
        public readonly Board Board;
        public readonly List<Node> Children = new();
        private readonly Random m_Random;
        
        public int NumWins;
        public double Score;
        public double NumVisits;
        
        public int RaveVisits;
        public double RaveScore;
        
        private const int RAVE_CONST = 200;
        
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
            return Children[new Random().Next(Children.Count)];
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
            return node.NumWins / node.NumVisits;
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
                newBoard.ChangePlayerToMove();
                var newNode = new Node(newBoard);
                notes.Add(newNode);
            }
            return notes;
        }
            
        public void RandomMove() 
        {
            Span<Move> legalMoves = stackalloc Move[Board.MAX_LEGAL_MOVES];
            Board.GenerateLegalMoves(ref legalMoves);
            if (legalMoves.Length == 0)
            {
                Board.ChangePlayerToMove();
                return;
            }
            
            var randomMove = legalMoves[m_Random.Next(legalMoves.Length)];
            Board.MakeMove(randomMove);
            Board.ChangePlayerToMove();
        }
        
        public (int player, Move move) RandomMoveRave()
        {
            var player = Board.GetCurrentPlayer();
            Span<Move> legalMoves = stackalloc Move[Board.MAX_LEGAL_MOVES];
            Board.GenerateLegalMoves(ref legalMoves);
            if (legalMoves.Length == 0)
            {
                Board.ChangePlayerToMove();
                return (player, Move.NULLMOVE);
            }
            
            var randomMove = legalMoves[m_Random.Next(legalMoves.Length)];
            Board.MakeMove(randomMove);
            Board.ChangePlayerToMove();
            return (player, randomMove);
        }
        
        public double CalculateUct()
        {
            if (NumVisits == 0) 
                return int.MaxValue;
            return (Score / NumVisits) + Math.Sqrt(2.0) * Math.Sqrt(Math.Log(Parent.NumVisits) / NumVisits);
        }
        
        public double CalculateUctRave()
        {
            // TODO: Fix rave score / rave selection of notes. (Do the math!)
            if (NumVisits == 0) 
                return int.MaxValue;

            var alpha = Math.Max(0, (RAVE_CONST - NumVisits) / RAVE_CONST);
            var utc = Score / NumVisits + Math.Sqrt(2.0) * Math.Sqrt(Math.Log(Parent.NumVisits) / NumVisits);
            
            double amaf;
            if (RaveVisits != 0)
                amaf = RaveScore / RaveVisits;
            else
                amaf = 0;

            return (1 - alpha) * utc + alpha * amaf;
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