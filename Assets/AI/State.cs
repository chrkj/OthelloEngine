using System;
using System.Collections.Generic;
using Othello.Core;

namespace Othello.AI
{
    public class State 
    {
        public Board Board;
        public int NumWins;
        public int NumVisits;

        public List<State> GetAllPossibleStates()
        {
            var states = new List<State>();
            foreach (var legalMove in MoveGenerator.GenerateLegalMoves(Board))
            {
                var newBoard = Board.Copy();
                newBoard.MakeMove(legalMove, MoveGenerator.GetCaptureIndices(legalMove, newBoard));
                newBoard.ChangePlayer();
                var newState = new State { Board = newBoard };
                states.Add(newState);
            }
            return states;
        }
            
        public void RandomPlay() 
        {
            var legalMoves = MoveGenerator.GenerateLegalMoves(Board);
            if (legalMoves.Count == 0)
            {
                Board.ChangePlayer();
                return;
            }
            var rand = new Random();
            var randomMove = legalMoves[rand.Next(legalMoves.Count)];
            var captures = MoveGenerator.GetCaptureIndices(randomMove, Board);
            Board.MakeMove(randomMove, captures);
            Board.ChangePlayer();
        }
            
    }
}