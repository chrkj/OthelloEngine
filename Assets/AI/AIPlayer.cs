using System.Linq;
using Othello.Core;
using UnityEngine;
using Random = System.Random;

namespace Othello.AI
{
    public class AIPlayer : Player
    {
        private bool _moveFound;
        private Move _chosenMove;
        private readonly ISearchEngine _searchEngine;
        
        public AIPlayer(Board board, int color) : base(board, color)
        {
            //_searchEngine = searchEngine;
        }
        
        
        public override void Update()
        {
            if (!_moveFound) return;
            boardUI.UnhighlightLegalMoves(legalMoves);
            var lastMove = board.GetLastMove();
            if (lastMove != null) boardUI.UnhighlightSquare(lastMove.targetSquare);
            boardUI.HighlightSquare(_chosenMove.targetSquare);
            ChooseMove(_chosenMove);
            _moveFound = false;
        }

        public override void NotifyTurnToMove()
        {
            legalMoves = MoveGenerator.GenerateLegalMoves(board);
            if (legalMoves.Count == 0)
            {
                MonoBehaviour.print("No legal move for " + board.GetCurrentColorToMove());
                NoLegalMove();
                return;
            }
            var rand = new Random();
            var randIndex = rand.Next(0, legalMoves.Count - 1);
            _chosenMove = new Move(legalMoves.Keys.ElementAt(randIndex), color, legalMoves.Values.ElementAt(randIndex));
            _moveFound = true;
            legalMoves = MoveGenerator.GenerateLegalMoves(board);
            boardUI.HighlightLegalMoves(legalMoves);
        }
    }
}