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
        private Dictionary<int, HashSet<int>> _legalMoves;

        public HumanPlayer(Board board, int color)
        {
            _board = board;
            _color = color;
            _mainCam = Camera.main;
            _boardUI = Object.FindObjectOfType<BoardUI>();
        }

        public override void Update()
        {
            HandleInput();
        }

        public override void NotifyTurnToMove()
        {
            _legalMoves = MoveGenerator.GenerateLegalMoves(_board);
            _boardUI.HighlightLegalMoves(_legalMoves);
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
            var lastMove = _board.GetLastMove();
            
            var isValidSquare = !Board.IsOutOfBounds(selectedFile, selectedRank) || _boardUI.HasSprite(selectedIndex);
            if (!isValidSquare) return;
            
            if (_legalMoves.Count == 0)
            {
                MonoBehaviour.print("No legal move for " + _board.GetCurrentColorToMove());
                ChangePlayer();
                return;
            }

            if (!_legalMoves.ContainsKey(selectedIndex)) return;
            
            _boardUI.UnhighlightLegalMoves(_legalMoves);
            if (lastMove != null) _boardUI.UnhighlightSquare(lastMove.targetSquare);
            _boardUI.HighlightSquare(selectedIndex);

            var chosenMove = new Move(selectedIndex, _color, _legalMoves[selectedIndex]);
            ChooseMove(chosenMove);
        }
        
    }
}