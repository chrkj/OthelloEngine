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
        private Board _board;
        private BoardUI _boardUI;
        private State _gameState;
        private Player _playerToMove;
        private Player _blackPlayer;
        private Player _whitePlayer;
        private Player _whitePlayerNextGame;
        private Player _blackPlayerNextGame;
        private int _playerToStartNextGame;
        private bool _lastPlayerHadNoMove;
        private enum State { Playing, GameOver, Idle }
        private enum PlayerType { Human = 0, Minimax = 1, MCTS = 2 }

        private void Start()
        {
            _boardUI = FindObjectOfType<BoardUI>();
            MoveGenerator.PrecomputeData();
            Setup();
        }

        private void Setup()
        {
            _playerToStartNextGame = Piece.Black;
            _board = new Board(_playerToStartNextGame);
            _board.LoadStartPosition();
            _boardUI.UpdateBoardUI(_board);

            blackAiDepthMinimax.text = "5";
            whiteAiDepthMinimax.text = "5";
            blackAiIterationsMcts.text = "500";
            whiteAiIterationsMcts.text = "500";
            PlayerSelection(ref _whitePlayerNextGame, (int)PlayerType.Human, Piece.White);
            PlayerSelection(ref _blackPlayerNextGame, (int)PlayerType.Human, Piece.Black);
            whitePiecePlayer.onValueChanged.AddListener(delegate
            {
                PlayerSelection(ref _whitePlayerNextGame, whitePiecePlayer.value, Piece.White);
            });
            blackPiecePlayer.onValueChanged.AddListener(delegate
            {
                PlayerSelection(ref _blackPlayerNextGame, blackPiecePlayer.value, Piece.Black);
            });
            whiteAiDepthMinimax.onValueChanged.AddListener(delegate
            {
                PlayerSelection(ref _whitePlayerNextGame, whitePiecePlayer.value, Piece.White);
            });
            blackAiDepthMinimax.onValueChanged.AddListener(delegate
            {
                PlayerSelection(ref _blackPlayerNextGame, blackPiecePlayer.value, Piece.Black);
            });
            whiteAiIterationsMcts.onValueChanged.AddListener(delegate
            {
                PlayerSelection(ref _whitePlayerNextGame, whitePiecePlayer.value, Piece.White);
            });
            blackAiIterationsMcts.onValueChanged.AddListener(delegate
            {
                PlayerSelection(ref _blackPlayerNextGame, blackPiecePlayer.value, Piece.Black);
            });
            showLegalMoves.onValueChanged.AddListener(delegate { ToggleLegalMoves(showLegalMoves.isOn); });
            playerToStart.onValueChanged.AddListener(delegate { SetStartingPlayer(playerToStart.value); });
            _gameState = State.Idle;
        }

        public void NewGame()
        {
            _board.ResetBoard(_playerToStartNextGame);
            _board.LoadStartPosition();
            _boardUI.UpdateBoardUI(_board);
            _whitePlayer = (Player)_whitePlayerNextGame.Clone();
            _blackPlayer = (Player)_blackPlayerNextGame.Clone();
            _playerToMove = _playerToStartNextGame == Piece.White ? _whitePlayer : _blackPlayer;
            _gameState = State.Playing;
            _playerToMove.NotifyTurnToMove();
        }

        private void Update()
        {
            switch (_gameState)
            {
                case State.Playing:
                    _playerToMove.Update();
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
                player.ONMoveChosen -= MakeMove;
                player.ONNoLegalMove -= NoLegalMove;
            }
            var inputFieldMCTS = playerColor == Piece.Black ? blackAiIterationsMcts : whiteAiIterationsMcts;
            var inputFieldMinimax = playerColor == Piece.Black ? blackAiDepthMinimax : whiteAiDepthMinimax;
            if (inputFieldMCTS.text.Length == 0) inputFieldMCTS.text = "1";
            if (inputFieldMinimax.text.Length == 0) inputFieldMinimax.text = "1";
            switch (playerType)
            {
                case (int)PlayerType.Human:
                    player = new HumanPlayer(_board, playerColor);
                    inputFieldMCTS.gameObject.SetActive(false);
                    inputFieldMinimax.gameObject.SetActive(false);
                    print("Player Type: Human, Player color: " + Piece.GetPlayerAsString(playerColor));
                    break;
                case (int)PlayerType.Minimax:
                    var depth = Int32.Parse(inputFieldMinimax.text);
                    if (depth < 1) depth = 1;
                    player = new AIPlayer(_board, playerColor, new MiniMax(depth));
                    inputFieldMinimax.gameObject.SetActive(true);
                    inputFieldMCTS.gameObject.SetActive(false);
                    print("Player Type: AIMinimax, Player color: " + Piece.GetPlayerAsString(playerColor) + ", depth = " + depth);
                    break;
                case (int)PlayerType.MCTS:
                    var iterations = Int32.Parse(inputFieldMCTS.text);
                    if (iterations < 1) iterations = 1;
                    player = new AIPlayer(_board, playerColor, new MonteCarloTreeSearch(iterations));
                    inputFieldMCTS.gameObject.SetActive(true);
                    inputFieldMinimax.gameObject.SetActive(false);
                    print("Player Type: AIMCTS, Player color: " + Piece.GetPlayerAsString(playerColor) + ", iterations = " + iterations);
                    break;
            }
            if (player == null) return;
            player.ONMoveChosen += MakeMove;
            player.ONNoLegalMove += NoLegalMove;
        }

        private void MakeMove(int move)
        {
            var captures = MoveGenerator.GetCaptureIndices(move, _board);
            _board.MakeMove(move, captures);
            _boardUI.MakeMove(move, captures, _board);
            ChangePlayer();
            _boardUI.UpdateUI(_board);
            _lastPlayerHadNoMove = false;
        }

        private void ChangePlayer()
        {
            _board.ChangePlayer();
            switch (_gameState)
            {
                case State.Playing:
                    _playerToMove = (_board.GetCurrentPlayer() == Piece.White) ? _whitePlayer : _blackPlayer;
                    _playerToMove.NotifyTurnToMove();
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
            if (_lastPlayerHadNoMove) _gameState = State.GameOver;
            _lastPlayerHadNoMove = true;
            ChangePlayer();
        }
        
        private void ToggleLegalMoves(bool isOn)
        {
            _boardUI.ToggleLegalMoves(isOn);
        }
        
        private void SetStartingPlayer(int value)
        {
            _playerToStartNextGame = value == 0 ? Piece.Black : Piece.White;
        }

    }
}