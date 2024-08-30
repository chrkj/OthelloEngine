using System.Collections;
using UnityEngine;

using Othello.UI;
using Othello.Utility;

namespace Othello.Core
{
    public class GameManager : SingletonMono<GameManager>
    {
        public int Draws;
        public int BlackWins;
        public int WhiteWins;
        public int NumSimsRan;
        public ComputeShader ComputeShader;
        
        private Board m_Board;
        private Player m_PlayerToMove;
        private Player m_BlackPlayer;
        private Player m_WhitePlayer;
        private State m_GameState;
        private bool m_LastPlayerHadNoMove;
        private Move m_LastMove = Move.NULLMOVE;
        [SerializeField] private int m_TimeBetweenGamesSimulation = 1;

        private enum State { Playing, GameOver, Idle }

        protected override void Awake()
        {
            base.Awake();
            Application.runInBackground = true;
        }

        private void Start()
        {
            m_Board = new Board();
            m_Board.LoadStartPosition();
            BoardUI.Instance.InitBoard();
            BoardUI.Instance.UpdateBoard(m_Board);
            MenuUI.Instance.Setup(m_Board);
            m_GameState = State.Idle;
            ResetSimCount();
        }
        
        private void Update()
        {
            switch (m_GameState)
            {
                case State.Playing:
                    m_PlayerToMove.Update();
                    MenuUI.Instance.UpdateMenu(m_Board);
                    break;
                case State.GameOver:
                    if (NumSimsRan < MenuUI.Instance.NumSimsToRun)
                    {
                        NumSimsRan++;
                        StartCoroutine(StartNewGameAfterSeconds(m_TimeBetweenGamesSimulation));
                        m_GameState = State.Idle;
                    }
                    break;
                case State.Idle:
                    break;
            }
        }

        public void NewGame()
        {
            MenuUI.Instance.CancelAiThread();
            m_Board.ResetBoard(MenuUI.Instance.PlayerToStartNextGame);
            m_Board.LoadStartPosition();
            BoardUI.Instance.UpdateBoard(m_Board);
            BoardUI.Instance.UnhighlightAll();
            Console.Clear();
            TryUnsubscribeEvents();
            MenuUI.Instance.PlayerSelection(Piece.WHITE);
            MenuUI.Instance.PlayerSelection(Piece.BLACK);
            m_WhitePlayer = (Player)MenuUI.Instance.WhitePlayerNextGame.Clone();
            m_BlackPlayer = (Player)MenuUI.Instance.BlackPlayerNextGame.Clone();
            SubscribeEvents();
            m_PlayerToMove = (MenuUI.Instance.PlayerToStartNextGame == Piece.WHITE) ? m_WhitePlayer : m_BlackPlayer;
            m_GameState = State.Playing;
            m_PlayerToMove.NotifyTurnToMove();
        }

        public void ResetSimCount()
        {
            NumSimsRan = 1;
            BlackWins = 0;
            WhiteWins = 0;
            Draws = 0;
        }

        private void SubscribeEvents()
        {
            m_WhitePlayer.OnMoveChosen += MakeMove;
            m_WhitePlayer.OnNoLegalMove += NoLegalMove;
            m_BlackPlayer.OnMoveChosen += MakeMove;
            m_BlackPlayer.OnNoLegalMove += NoLegalMove;
        }

        private void TryUnsubscribeEvents()
        {
            if (m_BlackPlayer != null)
            {
                m_BlackPlayer.OnMoveChosen -= MakeMove;
                m_BlackPlayer.OnNoLegalMove -= NoLegalMove;
            }
            if (m_WhitePlayer != null)
            {
                m_WhitePlayer.OnMoveChosen -= MakeMove;
                m_WhitePlayer.OnNoLegalMove -= NoLegalMove;
            }
        }
        
        private IEnumerator StartNewGameAfterSeconds(int seconds)
        {
            yield return new WaitForSeconds(seconds);
            NewGame();
        }

        private void MakeMove(Move move)
        {
            m_Board.MakeMove(move);
            m_LastMove = move;
            ChangePlayer();
            m_LastPlayerHadNoMove = false;
            BoardUI.Instance.UpdateBoard(m_Board);
            BoardUI.Instance.HighlightLastMove(m_LastMove);
        }

        private void NoLegalMove()
        {
            if (IsGameOver())
                return;
            m_LastPlayerHadNoMove = true;
            ChangePlayer();
        }

        private bool IsGameOver()
        {
            if (!m_LastPlayerHadNoMove) 
                return false;
            m_GameState = State.GameOver;
            var winner = "";
            if (m_Board.GetWinner() == Piece.BLACK)
            { 
                winner = "Black";
                BlackWins++;
            }
            else if (m_Board.GetWinner() == Piece.WHITE)
            { 
                winner = "White";
                WhiteWins++;
            }
            else if (m_Board.GetWinner() == 0)
            { 
                winner = "Draw";
                Draws++;
            }
            Console.Log("---------------- Winner: " + winner + " ----------------");
            Console.Log("----------------------------------------------------");
            return true;
        }

        private void ChangePlayer()
        {
            m_Board.ChangePlayer();
            m_PlayerToMove = (m_Board.GetCurrentPlayer() == Piece.WHITE) ? m_WhitePlayer : m_BlackPlayer;
            m_PlayerToMove.NotifyTurnToMove();
        }
    }
}