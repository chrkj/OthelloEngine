using System;
using UnityEngine;

namespace Othello.Core
{
    public class HumanPlayer : Player
    {
        
        private readonly Camera m_mainCam;

        public HumanPlayer(Board board) : base(board)
        {
            m_mainCam = Camera.main;
        }

        public override void Update()
        {
            HandleInput();
        }

        public override void NotifyTurnToMove()
        {
            m_legalMoves = m_Board.GenerateLegalMoves();
            m_BoardUI.HighlightLegalMoves(m_legalMoves);
            if (m_legalMoves.Count != 0) return;
            MonoBehaviour.print("No legal move for " + m_Board.GetCurrentPlayerAsString());
            NoLegalMove();
        }

        private void HandleInput()
        {
            var mousePosition = m_mainCam.ScreenToWorldPoint(Input.mousePosition);
            HandlePieceSelection(mousePosition);
        }

        private void HandlePieceSelection(Vector3 mousePosition)
        {
            if (!Input.GetButtonDown("Fire1")) return;
            
            var selectedFile = (int) Math.Floor(mousePosition.x) + 4;
            var selectedRank = (int) Math.Floor(mousePosition.y) + 4;
            if (Board.IsOutOfBounds(selectedFile, selectedRank)) return;
            var selectedIndex = Board.GetIndex(selectedFile, selectedRank);
            var lastMove = m_Board.GetLastMove();
            
            var isValidSquare = !Board.IsOutOfBounds(selectedFile, selectedRank) || m_BoardUI.HasSprite(selectedIndex);
            if (!isValidSquare) return;

            if (!m_legalMoves.Contains(new Move(selectedIndex))) return;
            
            m_BoardUI.UnhighlightLegalMoves(m_legalMoves);
            if (lastMove != null) m_BoardUI.UnhighlightSquare(lastMove.Index);
            m_BoardUI.HighlightSquare(selectedIndex);

            var chosenMove = new Move(selectedIndex);
            ChooseMove(chosenMove);
        }
        
    }
}