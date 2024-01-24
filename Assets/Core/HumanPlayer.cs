using System;
using System.Collections.Generic;
using UnityEngine;

namespace Othello.Core
{
    public class HumanPlayer : Player
    {
        private List<Move> m_legalMoves;
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
            if (m_legalMoves.Count == 0)
            {
                NoLegalMove();
                return;
            }
            m_BoardUI.HighlightLegalMoves(m_legalMoves);
        }

        private void HandleInput()
        {
            var mousePosition = m_mainCam.ScreenToWorldPoint(Input.mousePosition);
            HandlePieceSelection(mousePosition);
        }

        private void HandlePieceSelection(Vector3 mousePosition)
        {
            if (!Input.GetButtonDown("Fire1")) 
                return;
            
            var selectedFile = (int) Math.Floor(mousePosition.x) + 4;
            var selectedRank = (int) Math.Floor(mousePosition.y) + 4;
            if (Board.IsOutOfBounds(selectedFile, selectedRank))
                return;
            
            var selectedIndex = Board.GetIndex(selectedFile, selectedRank);
            var isValidSquare = !Board.IsOutOfBounds(selectedFile, selectedRank) || !m_Board.IsEmpty(selectedIndex);
            if (!isValidSquare)
                return;

            if (!m_legalMoves.Contains(new Move(selectedIndex))) 
                return;
            
            var chosenMove = new Move(selectedIndex);
            Console.Log(m_Board.GetCurrentPlayerAsString() + " plays " + chosenMove.ToString());
            Console.Log("----------------------------------------------------");
            ChooseMove(chosenMove);
        }
        
    }
}