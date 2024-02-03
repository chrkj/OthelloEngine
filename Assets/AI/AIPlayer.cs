using System;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

using Othello.Core;
using Othello.UI;

namespace Othello.AI
{
    public class AIPlayer : Player
    {
        public static Stopwatch s_BlackTimeElapsed = new();
        public static Stopwatch s_WhiteTimeElapsed = new();
        
        public readonly CancellationTokenSource Cts = new();

        private bool m_MoveFound;
        private Move m_ChosenMove;
        private readonly ISearchEngine m_SearchEngine;

        public AIPlayer(Board board, ISearchEngine searchEngine) : base(board)
        {
            m_SearchEngine = searchEngine;
        }

        public override void Update()
        {
            if (!m_MoveFound)
                return;
            if (!MenuUI.Instance.AutoMove)
            {
                if (!Input.GetKeyDown(KeyCode.Space))
                    return;
                m_MoveFound = false;
                ChooseMove(m_ChosenMove);
            }
            else
            {
                m_MoveFound = false;
                ChooseMove(m_ChosenMove);
            }
        }

        public override void NotifyTurnToMove()
        {
            Span<Move> legalMoves = stackalloc Move[256];
            m_Board.GenerateLegalMovesStack(ref legalMoves);
            if (legalMoves.Length == 0)
            {
                NoLegalMove();
                return;
            }
            BoardUI.Instance.HighlightLegalMoves(legalMoves.ToArray().ToList());
            Task.Factory.StartNew(Search, 
                Cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private void Search()
        {
            StartStopwatch();
            m_ChosenMove = m_SearchEngine.StartSearch(m_Board);
            StopStopwatch();
            m_MoveFound = true;
        }

        private void StopStopwatch()
        {
            if (m_Board.GetCurrentPlayer() == Piece.BLACK)
            {
                BoardUI.Instance.BlackAiPlayerCalculating = false;
                s_BlackTimeElapsed.Stop();
            }
            else
            {
                BoardUI.Instance.WhiteAiPlayerCalculating = false;
                s_WhiteTimeElapsed.Stop();
            }
        }

        private void StartStopwatch()
        {
            if (m_Board.GetCurrentPlayer() == Piece.BLACK)
            {
                BoardUI.Instance.BlackAiPlayerCalculating = true;
                s_BlackTimeElapsed.Reset();
                s_BlackTimeElapsed.Start();
            }
            else
            {
                BoardUI.Instance.WhiteAiPlayerCalculating = true;
                s_WhiteTimeElapsed.Reset();
                s_WhiteTimeElapsed.Start();
            }
        }
    }
}