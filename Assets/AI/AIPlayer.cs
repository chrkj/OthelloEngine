using System.Threading;
using UnityEngine;

using Othello.Core;

namespace Othello.AI
{
    public class AIPlayer : Player
    {
        private bool m_moveFound;
        private Move m_chosenMove;
        private readonly ISearchEngine m_searchEngine;
        
        public AIPlayer(Board board, ISearchEngine searchEngine) : base(board)
        {
            m_searchEngine = searchEngine;
        }
        
        public override void Update()
        {
            //if (!Input.GetButtonDown("Fire1")) return;
            if (!m_moveFound) return;
            m_BoardUI.UnhighlightLegalMoves(m_legalMoves);
            var lastMove = m_Board.GetLastMove();
            if (lastMove != null) m_BoardUI.UnhighlightSquare(lastMove.Index);
            m_BoardUI.HighlightSquare(m_chosenMove.Index);
            m_moveFound = false;
            ChooseMove(m_chosenMove);
        }

        public override void NotifyTurnToMove()
        {
            m_legalMoves = MoveGenerator.GenerateLegalMoves(m_Board);
            if (m_legalMoves.Count == 0)
            {
                MonoBehaviour.print("No legal move for " + m_Board.GetCurrentPlayerAsString());
                NoLegalMove();
                return;
            }
            var engineThread = new Thread(StartSearch);
            engineThread.Start();
            m_BoardUI.HighlightLegalMoves(m_legalMoves);
        }

        private void StartSearch()
        {
            m_chosenMove = m_searchEngine.StartSearch(m_Board);
            m_moveFound = true;
        }
    }
}