using System.Collections.Generic;

namespace Othello.Core
{
    public class Board
    {
        private readonly int[] _board = new int[64];
        private readonly Stack<Move> _history = new Stack<Move>();
        
        private bool _isWhiteToMove = true;
        private int _currentPlayerColor = Piece.White;
        private int _currentOpponentColor = Piece.Black;

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

        public int GetColorToMove()
        {
            return _currentPlayerColor;
        }
        
        public void MakeMove(Move move, HashSet<int> captures)
        {
            _history.Push(move);
            _board[move.TargetSquare] = move.Piece;
            
            foreach (var index in captures)
                _board[index] = move.Piece;
        }

        public Move GetLastMove()
        {
            if (_history.Count == 0) return null;
            return _history.Peek();
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
                    if ( GetPiece(file, rank) == Piece.Empty )
                        emptyIndices.Add(GetBoardIndex(file, rank));
                }
            }
            return emptyIndices;
        }

        public static bool IsOutOfBounds(int index)
        {
            return index < 0 || index > 63;
        }

        public bool IsOpponentPiece(int index)
        {
            return Piece.IsSameColor(_board[index], _currentOpponentColor);
        }
        
        public bool IsFriendlyPiece(int index)
        {
            return Piece.IsSameColor(_board[index], _currentPlayerColor);
        }

        public void ChangePlayer()
        {
            _isWhiteToMove = !_isWhiteToMove;
            _currentPlayerColor = (_isWhiteToMove) ? Piece.White : Piece.Black;
            _currentOpponentColor = (_isWhiteToMove) ? Piece.Black : Piece.White;
        }

        public string CurrentPlayerAsString()
        {
            return _isWhiteToMove ? "White" : "Black";
        }
        
        public int GetCurrentColorToMove()
        {
            return _currentPlayerColor;
        }

        public string GetPieceCount(int color)
        {
            var count = 0;
            foreach (var square in _board)
                if (square == color)
                    count++;
            return count.ToString();
        }

    }
}