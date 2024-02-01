using System;
using System.Collections;
using UnityEngine;

using Othello.UI;

namespace Othello.Core
{
    public class GameManager : MonoBehaviour
    {
        private Board m_Board;
        private BoardUI m_BoardUI;

        private Player m_PlayerToMove;
        private Player m_BlackPlayer;
        private Player m_WhitePlayer;

        private State m_GameState;
        private Settings m_Settings;
        public static int s_GamesToRun;
        public static int s_BlackWins;
        public static int s_WhiteWins;
        public static int s_Draws;

        private bool m_LastPlayerHadNoMove;
        private enum State { Playing, GameOver, Idle }

        private void Awake()
        {
            Application.runInBackground = true;
            m_Settings = GetComponent<Settings>();
            m_BoardUI = FindObjectOfType<BoardUI>();
            MoveData.PrecomputeData();
        }

        private void Start()
        {
            m_Board = new Board();
            m_Board.LoadStartPosition();
            m_BoardUI.UpdateBoard(m_Board);
            m_Settings.Setup(m_Board, m_BoardUI);
            m_GameState = State.Idle;
            ResetSim();
        }

        public void NewGame()
        {
            m_Board.ResetBoard(m_Settings.PlayerToStartNextGame);
            m_Board.LoadStartPosition();
            m_BoardUI.UpdateBoard(m_Board);
            m_BoardUI.UnhighlightAll();
            Console.Clear();
            TryUnsubscribeEvents();
            m_Settings.PlayerSelection(Piece.White);
            m_Settings.PlayerSelection(Piece.Black);
            m_WhitePlayer = (Player)m_Settings.WhitePlayerNextGame.Clone();
            m_BlackPlayer = (Player)m_Settings.BlackPlayerNextGame.Clone();
            SubscribeEvents();
            m_PlayerToMove = (m_Settings.PlayerToStartNextGame == Piece.White) ? m_WhitePlayer : m_BlackPlayer;
            m_GameState = State.Playing;
            m_PlayerToMove.NotifyTurnToMove();
        }

        public void ResetSim()
        {
            s_GamesToRun = 1;
            s_BlackWins = 0;
            s_WhiteWins = 0;
            s_Draws = 0;
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
                    m_BoardUI.UpdateUI(m_Board, m_Settings);
                    break;
                case State.GameOver:
                    if (s_GamesToRun < int.Parse(m_Settings.numGamesForSim.text))
                    {
                        s_GamesToRun++;
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
            ChangePlayer();
            m_BoardUI.UpdateBoard(m_Board);
            m_LastPlayerHadNoMove = false;
        }

        private void NoLegalMove()
        {
            if (m_LastPlayerHadNoMove)
            {
                m_GameState = State.GameOver;
                var winner = "";
                if (m_Board.GetWinner() == Piece.Black)
                { 
                    winner = "Black";
                    s_BlackWins++;
                }
                else if (m_Board.GetWinner() == Piece.White)
                { 
                    winner = "White";
                    s_WhiteWins++;
                }
                else if (m_Board.GetWinner() == 0)
                { 
                    winner = "Draw";
                    s_Draws++;
                }
                Console.Log("---------------- Winner: " + winner + " ----------------");
                Console.Log("----------------------------------------------------");
            }
            m_LastPlayerHadNoMove = true;
            ChangePlayer();
        }

        private void ChangePlayer()
        {
            m_Board.ChangePlayer();
            switch (m_GameState)
            {
                case State.Playing:
                    m_PlayerToMove = (m_Board.GetCurrentPlayer() == Piece.White) ? m_WhitePlayer : m_BlackPlayer;
                    m_PlayerToMove.NotifyTurnToMove();
                    break;
                case State.GameOver:
                    // Handle winning animation
                    break;
                case State.Idle:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    }
}