using Othello.Core;
using UnityEngine;

namespace Othello.AI
{
    public class AIPlayer : Player
    {
        private bool _moveFound;
        private Move _chosenMove;
        private readonly ISearchEngine _searchEngine;
        
        public AIPlayer(Board board, int color) : base(board, color)
        {
            _searchEngine = new MiniMax();
        }
        
        
        public override void Update()
        {
            if (!_moveFound) return;
            boardUI.UnhighlightLegalMoves(legalMoves);
            var lastMove = board.GetLastMove();
            if (lastMove != null) boardUI.UnhighlightSquare(lastMove.targetSquare);
            boardUI.HighlightSquare(_chosenMove.targetSquare);
            _moveFound = false;
            ChooseMove(_chosenMove);
        }

        public override void NotifyTurnToMove()
        {
            legalMoves = MoveGenerator.GenerateLegalMoves(board);
            if (legalMoves.Count == 0)
            {
                MonoBehaviour.print("No legal move for " + board.CurrentPlayerAsString());
                NoLegalMove();
                return;
            }
            _chosenMove = _searchEngine.StartSearch(board);
            _moveFound = true;
            boardUI.HighlightLegalMoves(legalMoves);
        }
    }
}