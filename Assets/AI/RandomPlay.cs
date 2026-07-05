using System;

using Othello.Core;
using UnityEngine;
using Console = Othello.UI.Console;
using Random = System.Random;

namespace Othello.AI
{
    public class RandomPlay : ISearchEngine
    {
        public SearchResult StartSearch(Board board)
        {
            return new SearchResult { BestMove = CalculateMove(board) };
        }

        private Move CalculateMove(Board board)
        {
            Span<Move> legalMoves = stackalloc Move[256];
            board.GenerateLegalMoves(ref legalMoves);
            var index = new Random().Next(legalMoves.Length);
            var move = legalMoves[index];
            Console.Log("■■■■■■■■■■■■■■■■■■■■■■■■■■■■", board.IsWhiteToMove ? Color.white : Color.black);
            Console.Log(board.GetCurrentPlayerAsString() + " plays " + move);
            Console.Log("■■■■■■■■■■■■■■■■■■■■■■■■■■■■", board.IsWhiteToMove ? Color.white : Color.black);
            return move;
        }
    }
}