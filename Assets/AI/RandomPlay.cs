using System;
using Othello.Core;

namespace Othello.AI
{
    public class RandomPlay : ISearchEngine
    {
        public int StartSearch(Board board)
        {
            return CalculateMove(board);
        }

        private static int CalculateMove(Board board)
        {
            var legalMoves = MoveGenerator.GenerateLegalMoves(board);
            var random = new Random();
            var index = random.Next(legalMoves.Count);
            return legalMoves[index];
        }
    }
}