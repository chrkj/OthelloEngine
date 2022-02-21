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
        public InputField blackAiDepth;
        public InputField whiteAiDepth;
        private Board _board;
        private BoardUI _boardUI;
        private State _gameState;
        private Player _playerTurn;
        private Player _blackPlayer;
        private Player _whitePlayer;
        private Player _whitePlayerNextGame;
        private Player _blackPlayerNextGame;
        private bool _lastPlayerHadNoMove;
        private enum State { Playing, GameOver }
        private enum PlayerType { Human = 0, Minimax = 1, MCTS = 2 }

        private void Start()
        {
            _boardUI = FindObjectOfType<BoardUI>();
            MoveGenerator.PrecomputeData();
            NewGame();
            _gameState = State.GameOver;
            GameSetup();
        }
        
        public void NewGame()
        {
            var startingPlayer = Piece.Black;
            _board = new Board(startingPlayer);
            _board.LoadStartPosition();
            _boardUI.UpdateBoardUI(_board);

            _whitePlayer = new HumanPlayer(_board, Piece.White);
            _blackPlayer = new HumanPlayer(_board, Piece.Black);
            _playerTurn = startingPlayer == Piece.White ? _whitePlayer : _blackPlayer;
            _gameState = State.Playing;
            
            _whitePlayer.ONMoveChosen += MakeMove;
            _blackPlayer.ONMoveChosen += MakeMove;
            _whitePlayer.ONNoLegalMove += NoLegalMove;
            _blackPlayer.ONNoLegalMove += NoLegalMove;
            
            _playerTurn.NotifyTurnToMove();
        }

        private void Update()
        {

            switch (_gameState)
            {
                case State.Playing:
                    _playerTurn.Update();
                    break;
                case State.GameOver:
                    //TODO
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void GameSetup()
        {
            whitePiecePlayer.onValueChanged.AddListener(delegate { HandlePlayerSelection(whitePiecePlayer.value, Piece.White); });

        }
        
        void HandlePlayerSelection(int playerType, int pieceType)
        {
            switch (playerType)
            {
                case (int)PlayerType.Human:
                    if (pieceType == Piece.White) 
                        _whitePlayerNextGame = new HumanPlayer(_board, pieceType);
                    else 
                        _blackPlayerNextGame = new HumanPlayer(_board, pieceType);
                    break;
                case (int)PlayerType.Minimax:
                    if (pieceType == Piece.White) 
                        _whitePlayerNextGame = new AIPlayer(_board, pieceType, new MiniMax(5));
                    else 
                        _blackPlayerNextGame = new AIPlayer(_board, pieceType, new MiniMax(5));
                    break;
                case (int)PlayerType.MCTS:
                    if (pieceType == Piece.White) 
                        _whitePlayerNextGame = new AIPlayer(_board, pieceType, new MonteCarloTreeSearch(500));
                    else 
                        _blackPlayerNextGame = new AIPlayer(_board, pieceType, new MonteCarloTreeSearch(500));
                    break;
            }
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
                    _playerTurn = (_board.GetCurrentPlayer() == Piece.White) ? _whitePlayer : _blackPlayer;
                    _playerTurn.NotifyTurnToMove();
                    break;
                case State.GameOver:
                    // Handle winning animation
                    print("GameOver");
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

    }
}