using System;
using System.Collections.Generic;
using System.Linq;

namespace Othello.Core
{
    public class Board
    {
        private readonly int[] _board;
        private readonly Stack<Move> _history = new Stack<Move>();
        
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
        
        public void LoadStartPosition()
        {
            _board[27] = Piece.Black;
            _board[28] = Piece.White;
            _board[35] = Piece.White;
            _board[36] = Piece.Black;
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
        
        
        public void MakeMove(Move move)
        {
            _history.Push(move);
            _board[move.targetSquare] = move.piece;
            foreach (var index in move.captures)
                _board[index] = move.piece;
        }

        public Move GetLastMove()
        {
            return _history.Count == 0 ? null : _history.Peek();
        }
        
        public static int GetBoardIndex(int file, int rank)
        {
            return rank * 8 + file;
        }
        
        public HashSet<int> GetEmptySquares()
        {
            var emptyIndices = new HashSet<int>();
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
            return IsOutOfBounds(GetBoardIndex(file, rank));
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
    }
}