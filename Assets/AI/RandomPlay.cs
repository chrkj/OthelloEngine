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
            Span<Move> legalMoves = stackalloc Move[256];
            board.GenerateLegalMovesStack(ref legalMoves);
            var index = new Random().Next(legalMoves.Length);
            var move = legalMoves[index];
            Console.Log(board.GetCurrentPlayerAsString() + " plays " + move);
            Console.Log("----------------------------------------------------");
            return move;
        }
    }
}