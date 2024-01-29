using System.Diagnostics;
using System.Threading;

using Othello.Core;
using Othello.UI;
using UnityEngine;

namespace Othello.AI
{
    public class AIPlayer : Player
    {
        private bool m_moveFound;
        private Move m_chosenMove;
        private readonly ISearchEngine m_searchEngine;

        public static Stopwatch s_BlackTimeElapsed = new Stopwatch();
        public static Stopwatch s_WhiteTimeElapsed = new Stopwatch();

        public AIPlayer(Board board, ISearchEngine searchEngine) : base(board)
        {
            m_searchEngine = searchEngine;
        }

        public override void Update()
        {
            if (!m_moveFound)
                return;
            if (!Settings.AutoMove)
            {
                if (!Input.GetKeyDown(KeyCode.Space))
                    return;
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
            StartStopwatch();
            m_chosenMove = m_searchEngine.StartSearch(m_Board);
            StopStopwatch();
            m_moveFound = true;
        }

        private void StopStopwatch()
        {
            if (m_Board.GetCurrentPlayer() == Piece.Black)
            {
                BoardUI.s_blackAiPlayerCalculating = false;
                s_BlackTimeElapsed.Stop();
            }
            else
            {
                BoardUI.s_whiteAiPlayerCalculating = false;
                s_WhiteTimeElapsed.Stop();
            }
        }

        private void StartStopwatch()
        {
            if (m_Board.GetCurrentPlayer() == Piece.Black)
            {
                BoardUI.s_blackAiPlayerCalculating = true;
                s_BlackTimeElapsed.Reset();
                s_BlackTimeElapsed.Start();
            }
            else
            {
                BoardUI.s_whiteAiPlayerCalculating = true;
                s_WhiteTimeElapsed.Reset();
                s_WhiteTimeElapsed.Start();
            }
        }
    }
}