using Othello.UI;
using UnityEngine;

namespace Othello.Core
{
    public class GameManager : MonoBehaviour
    {
        private enum State { Playing, Over }

        private Board _board;
        private Player _whitePlayer;
        private Player _blackPlayer;
        private Player _playerTurn;
        private State _gameState;
        private BoardUI _boardUI;
        public TMPro.TMP_Text playerToMoveUI;
        public TMPro.TMP_Text blackPieceCountUI;
        public TMPro.TMP_Text whitePieceCountUI;


        // TODO: CleanUp & Implement game over logic and UI
        // TODO: Implement new game functionality
        private void Start()
        {
            _boardUI = FindObjectOfType<BoardUI>();
            _boardUI.InitBoard();
            NewGame();
        }
        
        private void NewGame()
        {
            _board = new Board();
            _board.LoadStartPosition();

            _boardUI.UpdateUI(_board);
            
            _whitePlayer = new HumanPlayer(_board, Piece.White);
            _blackPlayer = new HumanPlayer(_board, Piece.Black);
            _playerTurn = _whitePlayer;
            _gameState = State.Playing;
            
            _whitePlayer.ONMoveChosen += MakeMove;
            _blackPlayer.ONMoveChosen += MakeMove;
            _whitePlayer.ONNoLegalMove += ChangePlayer;
            _blackPlayer.ONNoLegalMove += ChangePlayer;
            
            _playerTurn.NotifyTurnToMove();
            UpdateUI();
        }

        private void Update()
        {
            if (_gameState == State.Playing)
                _playerTurn.Update();
        }

        private void MakeMove(Move move)
        {
            var captures = MoveGenerator.GetCaptureIndices(move, _board);
            _board.MakeMove(move, captures);
            _boardUI.MakeMove(move, captures);
            ChangePlayer();
        }

        private void ChangePlayer()
        {
            _board.ChangePlayer();
            if (_gameState == State.Playing)
                _playerTurn = (_board.GetColorToMove() == Piece.White) ? _whitePlayer : _blackPlayer;
            _playerTurn.NotifyTurnToMove();
            UpdateUI();
        }

        private void UpdateUI()
        {
            playerToMoveUI.text = "Player: " + _board.CurrentPlayerAsString();
            blackPieceCountUI.text = $"Black: {_board.GetPieceCount(Piece.Black)}";
            whitePieceCountUI.text = $"White: {_board.GetPieceCount(Piece.White)}";
        }

    }
}