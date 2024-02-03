using System;
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
        
        private Board m_Board;
        private Player m_PlayerToMove;
        private Player m_BlackPlayer;
        private Player m_WhitePlayer;
        private State m_GameState;
        private bool m_LastPlayerHadNoMove;
        private Move m_LastMove = Move.NULLMOVE;
        private enum State { Playing, GameOver, Idle }

        protected override void Awake()
        {
            base.Awake();
            Application.runInBackground = true;
            MoveData.PrecomputeData();
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

        public void NewGame()
        {
            MenuUI.Instance.CancelGame();
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

            if (m_WhitePlayer == null)
                return;
            m_WhitePlayer.OnMoveChosen -= MakeMove;
            m_WhitePlayer.OnNoLegalMove -= NoLegalMove;
        }

        private void Update()
        {
            switch (m_GameState)
            {
                case State.Playing:
                    m_PlayerToMove.Update();
                    BoardUI.Instance.UpdateBoard(m_Board);
                    BoardUI.Instance.HighlightLastMove(m_LastMove);
                    MenuUI.Instance.UpdateManu(m_Board);
                    break;
                case State.GameOver:
                    if (NumSimsRan < MenuUI.Instance.NumSimsToRun)
                    {
                        NumSimsRan++;
                        StartCoroutine(StartNewGameAfterSeconds(1));
                        m_GameState = State.Idle;
                    }
                    break;
                case State.Idle:
                    break;
                default:
                    throw new NotImplementedException();
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
        }

        private void NoLegalMove()
        {
            if (m_LastPlayerHadNoMove)
            {
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
            }
            m_LastPlayerHadNoMove = true;
            ChangePlayer();
        }

        private void ChangePlayer()
        {
            m_Board.ChangePlayerToMove();
            switch (m_GameState)
            {
                case State.Playing:
                    m_PlayerToMove = (m_Board.GetCurrentPlayer() == Piece.WHITE) ? m_WhitePlayer : m_BlackPlayer;
                    m_PlayerToMove.NotifyTurnToMove();
                    break;
                case State.GameOver:
                    // TODO: Handle winning animation
                    break;
                case State.Idle:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    }
}