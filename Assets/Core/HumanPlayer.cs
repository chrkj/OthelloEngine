using System;
using System.Linq;
using UnityEngine;

using Othello.UI;

namespace Othello.Core
{
    public class HumanPlayer : Player
    {
        private Move[] m_LegalMoves;
        private readonly Camera m_MainCam;

        public HumanPlayer(Board board) : base(board)
        {
            m_MainCam = Camera.main;
        }

        public override void Update()
        {
            HandleInput();
        }

        public override void NotifyTurnToMove()
        {
            Span<Move> legalMoves = stackalloc Move[256];
            m_Board.GenerateLegalMoves(ref legalMoves);
            m_LegalMoves = legalMoves.ToArray();
            if (legalMoves.Length == 0)
            {
                NoLegalMove();
                return;
            }
            BoardUI.Instance.SetLegalMoves(legalMoves);
        }

        private void HandleInput()
        {
            var mousePosition = m_MainCam.ScreenToWorldPoint(Input.mousePosition);
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

            if (!m_LegalMoves.Contains(new Move(selectedIndex))) 
                return;
            
            var chosenMove = new Move(selectedIndex);
            Console.Log(m_Board.GetCurrentPlayerAsString() + " plays " + chosenMove);
            Console.Log("----------------------------------------------------");
            ChooseMove(chosenMove);
        }
        
    }
}