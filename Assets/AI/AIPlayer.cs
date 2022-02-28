using System.Threading;

using Othello.Core;
using UnityEngine;

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
            if (!m_moveFound) return;
            if (!Settings.AutoMove)
            {
                if (!Input.GetKeyDown(KeyCode.Space)) return;
                m_moveFound = false;
                ChooseMove(m_chosenMove);
            }
            else
            {
                m_moveFound = false;
                ChooseMove(m_chosenMove);
            }
        }

        public override void NotifyTurnToMove()
        {
            var legalMoves = m_Board.GenerateLegalMoves();
            if (legalMoves.Count == 0)
            {
                NoLegalMove();
                return;
            }
            m_BoardUI.HighlightLegalMoves(legalMoves);
            var engineThread = new Thread(StartSearch);
            engineThread.Start();
        }

        private void StartSearch()
        {
            m_chosenMove = m_searchEngine.StartSearch(m_Board);
            m_moveFound = true;
        }
    }
}