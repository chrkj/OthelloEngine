using System;

using Othello.Core;
using Console = Othello.Core.Console;

namespace Othello.AI
{
    public class RandomPlay : ISearchEngine
    {
        public Move StartSearch(Board board)
        {
            return CalculateMove(board);
        }

        private Move CalculateMove(Board board)
        {
            var legalMoves = board.GenerateLegalMoves();
            var index = new Random().Next(legalMoves.Count);
            var move = legalMoves[index];
            Console.Log(board.GetCurrentPlayerAsString() + " plays " + move.ToString());
            Console.Log("----------------------------------------------------");
            return move;
        }
    }
}