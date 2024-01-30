using System;
using UnityEngine;

using Othello.UI;

namespace Othello.Core
{
    public class GameManager : MonoBehaviour
    {
        private Board m_board;
        private BoardUI m_boardUI;

        private Player m_playerToMove;
        private Player m_blackPlayer;
        private Player m_whitePlayer;

        private State m_gameState;
        private Settings m_settings;
        public static int m_gamesToRun;
        public static int m_blackWins;
        public static int m_whiteWins;
        public static int m_draws;

        private bool m_lastPlayerHadNoMove;
        private enum State { Playing, GameOver, Idle }

        private void Awake()
        {
            Application.runInBackground = true;
            m_settings = GetComponent<Settings>();
            m_boardUI = FindObjectOfType<BoardUI>();
            MoveData.PrecomputeData();
        }

        private void Start()
        {
            m_board = new Board();
            m_board.LoadStartPosition();
            m_boardUI.UpdateBoard(m_board);
            m_settings.Setup(m_board, m_boardUI);
            m_gameState = State.Idle;
        }

        public void NewGame()
        {
            m_board.ResetBoard(m_settings.PlayerToStartNextGame);
            m_board.LoadStartPosition();
            m_boardUI.UpdateBoard(m_board);
            m_boardUI.UnhighlightAll();
            Console.Clear();
            TryUnsubscribeEvents();
            m_settings.PlayerSelection(Piece.White);
            m_settings.PlayerSelection(Piece.Black);
            m_whitePlayer = (Player)m_settings.WhitePlayerNextGame.Clone();
            m_blackPlayer = (Player)m_settings.BlackPlayerNextGame.Clone();
            SubscribeEvents();
            m_playerToMove = (m_settings.PlayerToStartNextGame == Piece.White) ? m_whitePlayer : m_blackPlayer;
            m_gameState = State.Playing;
            m_playerToMove.NotifyTurnToMove();
        }

        public void ResetSim()
        {
            m_gamesToRun = 0;
            m_blackWins = 0;
            m_whiteWins = 0;
            m_draws = 0;
        }

        private void SubscribeEvents()
        {
            m_whitePlayer.OnMoveChosen += MakeMove;
            m_whitePlayer.OnNoLegalMove += NoLegalMove;
            m_blackPlayer.OnMoveChosen += MakeMove;
            m_blackPlayer.OnNoLegalMove += NoLegalMove;
        }

        private void TryUnsubscribeEvents()
        {
            if (m_blackPlayer != null)
            {
                m_blackPlayer.OnMoveChosen -= MakeMove;
                m_blackPlayer.OnNoLegalMove -= NoLegalMove;
            }

            if (m_whitePlayer == null)
                return;
            m_whitePlayer.OnMoveChosen -= MakeMove;
            m_whitePlayer.OnNoLegalMove -= NoLegalMove;
        }

        private void Update()
        {
            switch (m_gameState)
            {
                case State.Playing:
                    m_playerToMove.Update();
                    m_boardUI.UpdateUI(m_board, m_settings);
                    break;
                case State.GameOver:
                    if (m_gamesToRun < int.Parse(m_settings.numGamesForSim.text))
                    {
                        m_gamesToRun++;
                        NewGame();
                    }
                    break;
                case State.Idle:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void MakeMove(Move move)
        {
            m_board.MakeMove(move);
            ChangePlayer();
            m_boardUI.UpdateBoard(m_board);
            m_lastPlayerHadNoMove = false;
        }

        private void NoLegalMove()
        {
            if (m_lastPlayerHadNoMove)
            {
                m_gameState = State.GameOver;
                var winner = "";
                if (m_board.GetWinner() == Piece.Black)
                { 
                    winner = "Black";
                    m_blackWins++;
                }
                else if (m_board.GetWinner() == Piece.White)
                { 
                    winner = "White";
                    m_whiteWins++;
                }
                else if (m_board.GetWinner() == 0)
                { 
                    winner = "Draw";
                    m_draws++;
                }
                Console.Log("---------------- Winner: " + winner + " ----------------");
                Console.Log("----------------------------------------------------");
            }
            m_lastPlayerHadNoMove = true;
            ChangePlayer();
        }

        private void ChangePlayer()
        {
            m_board.ChangePlayer();
            switch (m_gameState)
            {
                case State.Playing:
                    m_playerToMove = (m_board.GetCurrentPlayer() == Piece.White) ? m_whitePlayer : m_blackPlayer;
                    m_playerToMove.NotifyTurnToMove();
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