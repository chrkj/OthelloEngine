using System;

using Othello.Core;
using Random = System.Random;

namespace Othello.AI
{
    public class RandomPlay : ISearchEngine
    {
        private readonly Random m_Random = new();

        public SearchResult StartSearch(Board board)
        {
            Span<Move> legalMoves = stackalloc Move[256];
            board.GenerateLegalMoves(ref legalMoves);
            var move = legalMoves[m_Random.Next(legalMoves.Length)];
            return new SearchResult { BestMove = move };
        }
    }
}
