using System;
using Othello.AI;
using Othello.UI;
using UnityEngine;

namespace Othello.Core
{
    public class GameManager : MonoBehaviour
    {
        private enum State { Playing, Over }

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
            _board = new Board();
            _board.LoadStartPosition();
            _boardUI.UpdateBoardUI(_board);

            _whitePlayer = new HumanPlayer(_board, Piece.White);
            _blackPlayer = new AIPlayer(_board, Piece.Black);
            _playerTurn = _whitePlayer;
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
            _boardUI.UpdateUI(_board);
            _lastPlayerHadNoMove = false;
            ChangePlayer();
        }

        private void ChangePlayer()
        {
            _board.ChangePlayer();
            switch (_gameState)
            {
                case State.Playing:
                    _playerTurn = (_board.GetColorToMove() == Piece.White) ? _whitePlayer : _blackPlayer;
                    _playerTurn.NotifyTurnToMove();
                    break;
                case State.Over:
                    // Handle winning animation
                    print("GameOver");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void NoLegalMove()
        {
            if (_lastPlayerHadNoMove) _gameState = State.Over;
            _lastPlayerHadNoMove = true;
            ChangePlayer();
        }

    }
}