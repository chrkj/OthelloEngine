using System;
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
        public readonly Stopwatch TimeElapsed = new();
        public SearchResult LastResult { get; private set; }

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
            m_Board.GenerateLegalMoves(ref legalMoves);
            if (legalMoves.Length == 0)
            {
                NoLegalMove();
                return;
            }
            BoardUI.Instance.SetLegalMoves(legalMoves);
            
            if (m_SearchEngine is Mcts mcts)
            {
                if (mcts.Variant == MctsType.GpuParallel)
                    Search();
                else
                    Task.Factory.StartNew(Search, Cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            else
            {
                Task.Factory.StartNew(Search, Cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }

        private void Search()
        {
            StartStopwatch();
            LastResult = m_SearchEngine.StartSearch(m_Board);
            m_ChosenMove = LastResult.BestMove;
            StopStopwatch();
            m_MoveFound = true;
        }

        private void StopStopwatch()
        {
            if (m_Board.GetCurrentPlayer() == Piece.BLACK)
                BoardUI.Instance.BlackAiPlayerCalculating = false;
            else
                BoardUI.Instance.WhiteAiPlayerCalculating = false;
            TimeElapsed.Stop();
        }

        private void StartStopwatch()
        {
            if (m_Board.GetCurrentPlayer() == Piece.BLACK)
                BoardUI.Instance.BlackAiPlayerCalculating = true;
            else
                BoardUI.Instance.WhiteAiPlayerCalculating = true;
            TimeElapsed.Restart();
        }
    }
}