using System;
using System.Collections.Generic;
using Othello.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Othello.Core
{
    public class HumanPlayer : Player
    {
        private readonly int _color;
        private readonly Board _board;
        private readonly Camera _mainCam;
        private readonly BoardUI _boardUI;
        private HashSet<Move> _legalMoves;

        public HumanPlayer(Board board, int color)
        {
            _board = board;
            _boardUI = Object.FindObjectOfType<BoardUI>();
            _mainCam = Camera.main;
            _color = color;
        }

        public override void Update()
        {
            HandleInput();
        }

        public override void NotifyTurnToMove()
        {
            _legalMoves = MoveGenerator.GenerateLegalMoves(_board);
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
            var chosenMove = new Move(selectedIndex, _color);
            var lastMove = _board.GetLastMove();
            
            var isValidSquare = !Board.IsOutOfBounds(selectedFile, selectedRank) || _boardUI.HasSprite(selectedIndex);
            if (!isValidSquare) return;
            
            if (_legalMoves.Count == 0)
            {
                MonoBehaviour.print("No legal move for " + _board.GetCurrentColorToMove());
                ChangePlayer();
                return;
            }

            //TODO
            #region Handle highlighting (Refator to BoardUI)

            if (lastMove != null) _boardUI.UnhighlightSquare(lastMove.targetSquare);
            foreach (var legalMove in _legalMoves)
                _boardUI.HighlightSquare(legalMove.targetSquare);

            if (!_legalMoves.Contains(chosenMove)) return;

            foreach (var legalMove in _legalMoves)
                _boardUI.UnhighlightSquare(legalMove.targetSquare);

            if (lastMove != null) _boardUI.UnhighlightSquare(lastMove.targetSquare);
            _boardUI.HighlightSquare(selectedIndex);

            #endregion

            ChooseMove(chosenMove);
        }
        
    }
}