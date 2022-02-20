using System;
using System.Collections.Generic;
using Othello.UI;
using UnityEngine;
using Object = UnityEngine.Object;

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
            MonoBehaviour.print("No legal move for " + board.GetCurrentColorToMove());
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
            var selectedIndex = Board.GetBoardIndex(selectedFile, selectedRank);
            var lastMove = board.GetLastMove();
            
            var isValidSquare = !Board.IsOutOfBounds(selectedFile, selectedRank) || boardUI.HasSprite(selectedIndex);
            if (!isValidSquare) return;

            if (!legalMoves.ContainsKey(selectedIndex)) return;
            
            boardUI.UnhighlightLegalMoves(legalMoves);
            if (lastMove != null) boardUI.UnhighlightSquare(lastMove.targetSquare);
            boardUI.HighlightSquare(selectedIndex);

            var chosenMove = new Move(selectedIndex, color, legalMoves[selectedIndex]);
            ChooseMove(chosenMove);
        }
        
    }
}