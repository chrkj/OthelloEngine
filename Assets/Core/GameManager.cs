using System;
using Othello.AI;
using Othello.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Othello.Core
{
    public class GameManager : MonoBehaviour
    {
        public Toggle showLegalMoves;
        public Dropdown whitePiecePlayer;
        public Dropdown blackPiecePlayer;
        public Dropdown playerToStart;
        public InputField blackAiDepthMinimax;
        public InputField whiteAiDepthMinimax;
        public InputField blackAiIterationsMcts;
        public InputField whiteAiIterationsMcts;
        public Text debug;
        private Board m_board;
        private BoardUI m_boardUI;
        private State m_gameState;
        private Player m_playerToMove;
        private Player m_blackPlayer;
        private Player m_whitePlayer;
        private Player m_whitePlayerNextGame;
        private Player m_blackPlayerNextGame;
        private int m_playerToStartNextGame;
        private bool m_lastPlayerHadNoMove;
        private enum State { Playing, GameOver, Idle }

        public static long speed = 0;

        private enum PlayerType { Human = 0, Minimax = 1, Mcts = 2, Random = 3, MctsThreading = 4
        }

        private void Start()
        {
            m_boardUI = FindObjectOfType<BoardUI>();
            MoveData.PrecomputeData();
            Setup();
        }

        private void Setup()
        {
            m_playerToStartNextGame = Piece.Black;
            m_board = new Board(m_playerToStartNextGame);
            m_board.LoadStartPosition();
            m_boardUI.UpdateBoardUI(m_board);

            blackAiDepthMinimax.text = "5";
            whiteAiDepthMinimax.text = "5";
            blackAiIterationsMcts.text = "500";
            whiteAiIterationsMcts.text = "500";
            PlayerSelection(ref m_whitePlayerNextGame, (int)PlayerType.Human, Piece.White);
            PlayerSelection(ref m_blackPlayerNextGame, (int)PlayerType.Human, Piece.Black);
            whitePiecePlayer.onValueChanged.AddListener(delegate
            {
                PlayerSelection(ref m_whitePlayerNextGame, whitePiecePlayer.value, Piece.White);
            });
            blackPiecePlayer.onValueChanged.AddListener(delegate
            {
                PlayerSelection(ref m_blackPlayerNextGame, blackPiecePlayer.value, Piece.Black);
            });
            whiteAiDepthMinimax.onValueChanged.AddListener(delegate
            {
                PlayerSelection(ref m_whitePlayerNextGame, whitePiecePlayer.value, Piece.White);
            });
            blackAiDepthMinimax.onValueChanged.AddListener(delegate
            {
                PlayerSelection(ref m_blackPlayerNextGame, blackPiecePlayer.value, Piece.Black);
            });
            whiteAiIterationsMcts.onValueChanged.AddListener(delegate
            {
                PlayerSelection(ref m_whitePlayerNextGame, whitePiecePlayer.value, Piece.White);
            });
            blackAiIterationsMcts.onValueChanged.AddListener(delegate
            {
                PlayerSelection(ref m_blackPlayerNextGame, blackPiecePlayer.value, Piece.Black);
            });
            showLegalMoves.onValueChanged.AddListener(delegate { ToggleLegalMoves(showLegalMoves.isOn); });
            playerToStart.onValueChanged.AddListener(delegate { SetStartingPlayer(playerToStart.value); });
            m_gameState = State.Idle;
        }

        public void NewGame()
        {
            m_board.ResetBoard(m_playerToStartNextGame);
            m_board.LoadStartPosition();
            m_boardUI.UpdateBoardUI(m_board);
            m_boardUI.UnhighlightAll();
            m_whitePlayer = (Player)m_whitePlayerNextGame.Clone();
            m_blackPlayer = (Player)m_blackPlayerNextGame.Clone();
            m_playerToMove = m_playerToStartNextGame == Piece.White ? m_whitePlayer : m_blackPlayer;
            m_gameState = State.Playing;
            m_playerToMove.NotifyTurnToMove();
        }

        private void Update()
        {
            switch (m_gameState)
            {
                case State.Playing:
                    m_playerToMove.Update();
                    debug.text = speed.ToString();
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

        private void PlayerSelection(ref Player player, int playerType, int playerColor)
        {
            if (player != null)
            {
                player.OnMoveChosen -= MakeMove;
                player.OnNoLegalMove -= NoLegalMove;
            }
            var inputFieldMCTS = playerColor == Piece.Black ? blackAiIterationsMcts : whiteAiIterationsMcts;
            var inputFieldMinimax = playerColor == Piece.Black ? blackAiDepthMinimax : whiteAiDepthMinimax;
            if (inputFieldMCTS.text.Length == 0) inputFieldMCTS.text = "1";
            if (inputFieldMinimax.text.Length == 0) inputFieldMinimax.text = "1";
            int iterations;
            switch (playerType)
            {
                case (int)PlayerType.Human:
                    player = new HumanPlayer(m_board);
                    inputFieldMCTS.gameObject.SetActive(false);
                    inputFieldMinimax.gameObject.SetActive(false);
                    print("Player Type: Human, Player color: " + Piece.GetPlayerAsString(playerColor));
                    break;
                case (int)PlayerType.Minimax:
                    var depth = int.Parse(inputFieldMinimax.text);
                    if (depth < 1) depth = 1;
                    player = new AIPlayer(m_board, new MiniMax(depth));
                    inputFieldMinimax.gameObject.SetActive(true);
                    inputFieldMCTS.gameObject.SetActive(false);
                    print("Player Type: AI-Minimax, Player color: " + Piece.GetPlayerAsString(playerColor) + ", depth = " + depth);
                    break;
                case (int)PlayerType.Mcts:
                    iterations = int.Parse(inputFieldMCTS.text);
                    if (iterations < 1) iterations = 1;
                    player = new AIPlayer(m_board, new MonteCarloTreeSearch(iterations));
                    inputFieldMCTS.gameObject.SetActive(true);
                    inputFieldMinimax.gameObject.SetActive(false);
                    print("Player Type: AI-MCTS, Player color: " + Piece.GetPlayerAsString(playerColor) + ", iterations = " + iterations);
                    break;
                case (int)PlayerType.MctsThreading:
                    iterations = int.Parse(inputFieldMCTS.text);
                    if (iterations < 1) iterations = 1;
                    player = new AIPlayer(m_board, new MctsThreading(iterations));
                    inputFieldMCTS.gameObject.SetActive(true);
                    inputFieldMinimax.gameObject.SetActive(false);
                    print("Player Type: AI-MCTS-T, Player color: " + Piece.GetPlayerAsString(playerColor) + ", iterations = " + iterations);
                    break;
                case (int)PlayerType.Random:
                    player = new AIPlayer(m_board, new RandomPlay());
                    inputFieldMCTS.gameObject.SetActive(false);
                    inputFieldMinimax.gameObject.SetActive(false);
                    print("Player Type: AI-Random, Player color: " + Piece.GetPlayerAsString(playerColor));
                    break;
            }
            if (player == null) return;
            player.OnMoveChosen += MakeMove;
            player.OnNoLegalMove += NoLegalMove;
        }

        private void MakeMove(Move move)
        {
            m_board.MakeMove(move);
            ChangePlayer();
            m_boardUI.UpdateBoardUI(m_board);
            m_lastPlayerHadNoMove = false;
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
                    print("GameOver");
                    break;
                case State.Idle:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void NoLegalMove()
        {
            if (m_lastPlayerHadNoMove) m_gameState = State.GameOver;
            m_lastPlayerHadNoMove = true;
            ChangePlayer();
        }
        
        private void ToggleLegalMoves(bool isOn)
        {
            m_boardUI.ToggleLegalMoves(isOn);
        }
        
        private void SetStartingPlayer(int player)
        {
            m_playerToStartNextGame = player == 0 ? Piece.Black : Piece.White;
        }

    }
}