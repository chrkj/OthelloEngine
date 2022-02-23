using System;
using UnityEngine;

namespace Othello.Core
{
    public class HumanPlayer : Player
    {
        
        private readonly Camera _mainCam;

        public HumanPlayer(Board board, int color) : base(board, color)
        {
            _mainCam = Camera.main;
        }

        public override void Update()
        {
            HandleInput();
        }

        public override void NotifyTurnToMove()
        {
            legalMoves = MoveGenerator.GenerateLegalMoves(board);
            boardUI.HighlightLegalMoves(legalMoves);
            if (legalMoves.Count != 0) return;
            MonoBehaviour.print("No legal move for " + board.GetCurrentPlayerAsString());
            NoLegalMove();
        }

        private void HandleInput()
        {
            var mousePosition = _mainCam.ScreenToWorldPoint(Input.mousePosition);
            HandlePieceSelection(mousePosition);
        }

        private void HandlePieceSelection(Vector3 mousePosition)
        {
            if (!Input.GetButtonDown("Fire1")) return;
            
            var selectedFile = (int) Math.Floor(mousePosition.x) + 4;
            var selectedRank = (int) Math.Floor(mousePosition.y) + 4;
            if (Board.IsOutOfBounds(selectedFile, selectedRank)) return;
            var selectedIndex = Board.GetIndex(selectedFile, selectedRank);
            var lastMove = board.GetLastMove();
            
            var isValidSquare = !Board.IsOutOfBounds(selectedFile, selectedRank) || boardUI.HasSprite(selectedIndex);
            if (!isValidSquare) return;

            if (!legalMoves.Contains(selectedIndex)) return;
            
            boardUI.UnhighlightLegalMoves(legalMoves);
            if (lastMove != -1) boardUI.UnhighlightSquare(lastMove);
            boardUI.HighlightSquare(selectedIndex);

            var chosenMove = selectedIndex;
            ChooseMove(chosenMove);
        }
        
    }
}