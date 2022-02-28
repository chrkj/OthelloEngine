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
        private Settings Settings;
        
        private bool m_lastPlayerHadNoMove;
        private enum State { Playing, GameOver, Idle }

        private void Awake()
        {
            Settings = GetComponent<Settings>();
            m_boardUI = FindObjectOfType<BoardUI>();
            MoveData.PrecomputeData();
        }

        private void Start()
        {
            m_board = new Board();
            m_board.LoadStartPosition();
            m_boardUI.UpdateUI(m_board);
            Settings.Setup(m_board, m_boardUI);
            m_gameState = State.Idle;
        }

        public void NewGame()
        {
            m_board.ResetBoard(Settings.PlayerToStartNextGame);
            m_board.LoadStartPosition();
            m_boardUI.UpdateUI(m_board);
            m_boardUI.UnhighlightAll();
            Console.Clear();
            TryUnsubscribeEvents();
            m_whitePlayer = (Player)Settings.WhitePlayerNextGame.Clone();
            m_blackPlayer = (Player)Settings.BlackPlayerNextGame.Clone();
            SubscribeEvents();
            m_playerToMove = (Settings.PlayerToStartNextGame == Piece.White) ? m_whitePlayer : m_blackPlayer;
            m_gameState = State.Playing;
            m_playerToMove.NotifyTurnToMove();
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
            if (m_whitePlayer != null)
            {
                m_whitePlayer.OnMoveChosen -= MakeMove;
                m_whitePlayer.OnNoLegalMove -= NoLegalMove;
            }
        }

        private void Update()
        {
            switch (m_gameState)
            {
                case State.Playing:
                    m_playerToMove.Update();
                    break;
                case State.GameOver:
                    // TODO: Add gameover animation
                    break;
                case State.Idle:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void MakeMove(Move move)
        {
            m_board.MakeMove(move);
            ChangePlayer();
            m_boardUI.UpdateUI(m_board);
            m_lastPlayerHadNoMove = false;
        }
        
        private void NoLegalMove()
        {
            if (m_lastPlayerHadNoMove)
            {
                m_gameState = State.GameOver;
                var winner = "";
                if (m_board.GetWinner() == Piece.Black) winner = "Black";
                else if (m_board.GetWinner() == Piece.White) winner = "White";
                else if (m_board.GetWinner() == 0) winner = "Draw";
                Console.Log("-------------- Winner: " + winner + " --------------");
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