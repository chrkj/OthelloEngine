using System;

using Othello.Core;

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
            var legalMoves = MoveGenerator.GenerateLegalMoves(board);
            var random = new Random();
            var index = random.Next(legalMoves.Count);
            return legalMoves[index];
        }
    }
}