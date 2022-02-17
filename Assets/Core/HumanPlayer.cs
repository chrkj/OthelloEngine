using System;
using System.Collections.Generic;
using Othello.UI;
using UnityEngine;

namespace Othello.Core
{
    public class HumanPlayer : Player
    {
        private readonly int _color;
        private readonly Board _board;
        private readonly Camera _mainCam;
        private readonly BoardUI _boardUI;
        private HashSet<Move> _legalMoves;
        private readonly MoveGenerator _moveGenerator;

        public HumanPlayer(Board board, byte color)
        {
            _board = board;
            _boardUI = GameObject.FindObjectOfType<BoardUI>();
            _mainCam = Camera.main;
            _color = color;
            _moveGenerator = new MoveGenerator();
        }

        public override void Update()
        {
            HandleInput();
        }

        public override void NotifyTurnToMove()
        {
            _legalMoves = _moveGenerator.GenerateLegalMoves(_board);
        }

        private void HandleInput()
        {
            var mousePosition = _mainCam.ScreenToWorldPoint(Input.mousePosition);
            HandlePieceSelection(mousePosition);
        }

        private void HandlePieceSelection(Vector3 mousePosition)
        {
            if (Input.GetButtonDown("Fire1"))
            {
                var selectedFile = (int) Math.Floor(mousePosition.x) + 4;
                var selectedRank = (int) Math.Floor(mousePosition.y) + 4;
                var selectedIndex = Board.GetBoardIndex(selectedFile, selectedRank);

                var isValidSquare = 0 <= selectedFile & selectedFile < 8 & 0 <= selectedRank & selectedRank < 8;
                if (!isValidSquare || _boardUI.HasSprite(selectedIndex)) return;

                var chosenMove = new Move(selectedIndex, _color);

                if (_legalMoves.Count == 0)
                {
                    MonoBehaviour.print("No legal move for " + _board.GetCurrentColorToMove());
                    ChangePlayer();
                    return;
                }
                
                var lastMove = _board.GetLastMove();
                if (lastMove != null) _boardUI.UnhighlightSquare(lastMove.TargetSquare);
                foreach (var legalMove in _legalMoves)
                    _boardUI.HighlightSquare(legalMove.TargetSquare);

                
                if (!_legalMoves.Contains(chosenMove)) return;

                foreach (var legalMove in _legalMoves)
                    _boardUI.UnhighlightSquare(legalMove.TargetSquare);

                
                if (lastMove != null) _boardUI.UnhighlightSquare(lastMove.TargetSquare);
                
                _boardUI.HighlightSquare(selectedIndex);
                ChooseMove(chosenMove);
            }
        }
        
    }
}