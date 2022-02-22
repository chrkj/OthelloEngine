using System;
using System.Collections.Generic;
using System.Linq;

namespace Othello.Core
{
    public class Board
    {
        private readonly int[] _board;
        private readonly Stack<int> _history = new Stack<int>();
        
        private bool _isWhiteToMove;

        public Board(int startingPlayer)
        {
            _isWhiteToMove = startingPlayer == Piece.White;
            _board = new int[64];
        }
        
        public Board(Board board)
        {
            _board = new int[64];
            Array.Copy(board._board, _board, 64);
            _isWhiteToMove = board._isWhiteToMove;
        }

        public void ResetBoard()
        {
            for (int i = 0; i < _board.Length; i++)
                _board[i] = Piece.Empty;
            _history.Clear();
        }
        
        public void LoadStartPosition()
        {
            _board[27] = Piece.Black;
            _board[36] = Piece.Black;
            _board[28] = Piece.White;
            _board[35] = Piece.White;
        }

        public int GetPiece(int file, int rank)
        {
            return _board[GetBoardIndex(file, rank)];
        }

        public int GetCurrentPlayer()
        {
            return _isWhiteToMove ? Piece.White : Piece.Black;
        }

        public int GetCurrentOpponent()
        {
            return _isWhiteToMove ? Piece.Black : Piece.White;
        }
        
        
        public void MakeMove(int move, HashSet<int> captures)
        {
            _history.Push(move);
            _board[move] = GetCurrentPlayer();
            foreach (var capture in captures)
                _board[capture] = GetCurrentPlayer();
        }

        public int GetLastMove()
        {
            return _history.Count == 0 ? -1 : _history.Peek();
        }
        
        public static int GetBoardIndex(int file, int rank)
        {
            return rank * 8 + file;
        }
        
        public List<int> GetEmptySquares()
        {
            var emptyIndices = new List<int>();
            for (var file = 0; file < 8; file++){
                for (var rank = 0; rank < 8; rank++){
                    if (GetPiece(file, rank) == Piece.Empty)
                        emptyIndices.Add(GetBoardIndex(file, rank));
                }
            }
            return emptyIndices;
        }

        public static bool IsOutOfBounds(int index)
        {
            return index < 0 || index > 63;
        }
        
        public static bool IsOutOfBounds(int file, int rank)
        {
            return file < 0 || file > 7 && rank < 0 || rank > 7;
        }

        public bool IsOpponentPiece(int index)
        {
            return Piece.IsSameColor(_board[index], GetCurrentOpponent());
        }
        
        public bool IsFriendlyPiece(int index)
        {
            return Piece.IsSameColor(_board[index], GetCurrentPlayer());
        }

        public void ChangePlayer()
        {
            _isWhiteToMove = !_isWhiteToMove;
        }

        public string CurrentPlayerAsString()
        {
            return _isWhiteToMove ? "White" : "Black";
        }
        
        public string GetPieceCountAsString(int color)
        {
            var count = _board.Count(square => square == color);
            return count.ToString();
        }
        
        public int GetPieceCount(int color)
        {
            var count = _board.Count(square => square == color);
            return count;
        }
        

        public bool IsTerminalBoardState(Board board)
        {
            var legalMovesCurrentPlayer = MoveGenerator.GenerateLegalMoves(board).Count;
            ChangePlayer();
            var legalMovesCurrentOpponent = MoveGenerator.GenerateLegalMoves(board).Count;
            ChangePlayer();
            return legalMovesCurrentPlayer == 0 & legalMovesCurrentOpponent == 0;
        }

        public bool IsWinner(int currentPlayer)
        {
            return GetPieceCount(currentPlayer) > GetPieceCount(GetCurrentOpponent());
        }
        
        public int CheckStatus()
        {
            if (!IsTerminalBoardState(this)) return -1;
            return IsWinner(Piece.Black) ? Piece.Black : Piece.White;
        }

        public void SetStartingPlayer(int player)
        {
            _isWhiteToMove = player == Piece.White; 
        }
    }
}