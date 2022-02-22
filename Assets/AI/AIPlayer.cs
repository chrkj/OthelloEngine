using System.Threading;
using Othello.Core;
using UnityEngine;

namespace Othello.AI
{
    public class AIPlayer : Player
    {
        private bool _moveFound;
        private int _chosenMove;
        private readonly ISearchEngine _searchEngine;
        
        public AIPlayer(Board board, int color, ISearchEngine searchEngine) : base(board, color)
        {
            _searchEngine = searchEngine;
        }
        
        
        public override void Update()
        {
            //if (!Input.GetButtonDown("Fire1")) return;
            if (!_moveFound) return;
            boardUI.UnhighlightLegalMoves(legalMoves);
            var lastMove = board.GetLastMove();
            if (lastMove != -1) boardUI.UnhighlightSquare(lastMove);
            boardUI.HighlightSquare(_chosenMove);
            _moveFound = false;
            ChooseMove(_chosenMove);
        }

        public override void NotifyTurnToMove()
        {
            legalMoves = MoveGenerator.GenerateLegalMoves(board);
            if (legalMoves.Count == 0)
            {
                MonoBehaviour.print("No legal move for " + board.GetCurrentPlayerAsString());
                NoLegalMove();
                return;
            }
            var engineThread = new Thread(StartSearch);
            engineThread.Start();
            boardUI.HighlightLegalMoves(legalMoves);
        }

        private void StartSearch()
        {
            _chosenMove = _searchEngine.StartSearch(board);
            _moveFound = true;
        }
    }
}