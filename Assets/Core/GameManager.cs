using System;
using Othello.AI;
using Othello.UI;
using UnityEngine;

namespace Othello.Core
{
    public class GameManager : MonoBehaviour
    {
        private enum State { Playing, GameOver }

        private Board _board;
        private BoardUI _boardUI;
        private State _gameState;
        private Player _playerTurn;
        private Player _blackPlayer;
        private Player _whitePlayer;
        private bool _lastPlayerHadNoMove;


        private void Awake()
        {
            _boardUI = FindObjectOfType<BoardUI>();
        }

        private void Start()
        {
            MoveGenerator.PrecomputeData();
            _boardUI.InitBoard();
            NewGame();
        }
        
        private void NewGame()
        {
            var startingPlayer = Piece.Black;
            _board = new Board(startingPlayer);
            _board.LoadStartPosition();
            _boardUI.UpdateBoardUI(_board);

            _whitePlayer = new AIPlayer(_board, Piece.White, new MonteCarloTreeSearch(100));
            _blackPlayer = new AIPlayer(_board, Piece.Black, new MiniMax(2));
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
            if (_gameState == State.Playing)
                _playerTurn.Update();
            if (Input.GetKeyDown(KeyCode.R))
                NewGame();
        }

        private void MakeMove(Move move)
        {
            _board.MakeMove(move);
            _boardUI.MakeMove(move);
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