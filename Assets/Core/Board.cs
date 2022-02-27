using System.Collections.Generic;

namespace Othello.Core
{
    public class Board
    {
        private ulong m_pieces;
        private ulong m_colors;
        private Move m_lastMove;
        private bool m_isWhiteToMove;
        
        public Board(int playerToStart)
        {
            m_lastMove = null;
            m_isWhiteToMove = (playerToStart == Piece.White);
        }
        
        private Board() { }

        public Board Copy()
        {
            var copy = new Board
            {
                m_pieces = m_pieces, 
                m_colors = m_colors,
                m_lastMove = m_lastMove,
                m_isWhiteToMove = m_isWhiteToMove
            };
            return copy;
        }
        
        public void ResetBoard(int playerToStart)
        {
            m_pieces = 0;
            m_colors = 0;
            m_lastMove = null;
            m_isWhiteToMove = playerToStart == Piece.White;
        }

        public bool IsEmpty(int index)
        {
            return (m_pieces & (1UL << index)) == 0;
        }
        
        public bool IsTerminalBoardState()
        {
            var numLegalMovesCurrentPlayer = GenerateLegalMoves().Count;
            if (numLegalMovesCurrentPlayer != 0) return false;
            ChangePlayer();
            var numLegalMovesCurrentOpponent = GenerateLegalMoves().Count;
            ChangePlayer();
            return numLegalMovesCurrentOpponent == 0;
        }

        public void MakeMove(Move move)
        {
            m_lastMove = move;
            PlacePiece(move.Index, GetCurrentPlayer());
            var captures = GetCaptureIndices(move);
            foreach (var capture in captures)
                PlacePiece(capture.Index, GetCurrentPlayer());
        }
        
        public string GetPieceCountAsString(int player)
        {
            var count = GetPieceCount(player);
            return count.ToString();
        }
        

        public bool Equals(Board other)
        {
            return m_pieces == other.m_pieces && m_colors == other.m_colors && m_isWhiteToMove == other.m_isWhiteToMove;
        }
        
        public List<Move> GenerateLegalMoves()
        {
            var legalMoves = new List<Move>();
            var emptySquares = GetEmptySquares();
            foreach (var square in emptySquares)
                GenerateLegalMovesForSquare(square, legalMoves);
            return legalMoves;
        }
        
        public void LoadStartPosition()
        {
            PlacePiece(27, Piece.Black);
            PlacePiece(28, Piece.White);
            PlacePiece(35, Piece.White);
            PlacePiece(36, Piece.Black);
        }
        
        public static int GetIndex(int file, int rank)
        {
            return rank * 8 + file;
        }
        
        public int GetPieceColor(int file, int rank)
        {
            var index = GetIndex(file, rank);
            if (IsEmpty(index)) return 0;
            return (m_colors & (1UL << index)) == 0 ? Piece.White : Piece.Black;
        }
        
        public Move GetLastMove()
        {
            return m_lastMove;
        }
        
        public static bool IsOutOfBounds(int file, int rank)
        {
            return file < 0 | file > 7 | rank < 0 | rank > 7;
        }

        public int GetCurrentPlayer()
        {
            return m_isWhiteToMove ? Piece.White : Piece.Black;
        }
        
        public void ChangePlayer()
        {
            m_isWhiteToMove = !m_isWhiteToMove;
        }
        
        public string GetCurrentPlayerAsString()
        {
            return m_isWhiteToMove ? "White" : "Black";
        }

        public int GetPieceCount(int player)
        {
            var count = 0;
            for (var i = 0; i < 64; i++)
                if (!IsEmpty(i) && GetPieceColor(i) == player) count++;
            return count;
        }
        
        public int GetBoardState()
        {
            if (!IsTerminalBoardState()) return -1;
            return GetWinner();
        }

        private void PlacePiece(int index, int player)
        {
            m_pieces |= (1UL << index);
            switch (player)
            {
                case Piece.Black:
                    m_colors |= (1UL << index);
                    break;
                case Piece.White:
                    m_colors &= ~(1UL << index);
                    break;
            }
        }

        private int GetPieceColor(int index)
        {
            if (IsEmpty(index)) return 0;
            return (m_colors & (1UL << index)) == 0 ? Piece.White : Piece.Black;
        }

        private int GetCurrentOpponent()
        {
            return m_isWhiteToMove ? Piece.Black : Piece.White;
        }

        private List<int> GetEmptySquares()
        {
            var emptySquares = new List<int>();
            for (var i = 0; i < 64; i++)
                if (IsEmpty(i)) emptySquares.Add(i);
            return emptySquares;
        }

        private static bool IsOutOfBounds(int index)
        {
            return index < 0 || index > 63;
        }
        
        private bool IsOpponentPiece(int index)
        {
            return GetPieceColor(index) == GetCurrentOpponent();
        }

        private bool IsFriendlyPiece(int index)
        {
            return GetPieceColor(index) == GetCurrentPlayer();
        }

        private int GetWinner()
        {
            var playerPieceCount = GetPieceCount(GetCurrentPlayer());
            var opponentPieceCount = GetPieceCount(GetCurrentOpponent());
            if (playerPieceCount == opponentPieceCount) return 0;
            return playerPieceCount > opponentPieceCount ? GetCurrentPlayer() : GetCurrentOpponent();
        }

        private void GenerateLegalMovesForSquare(int square, ICollection<Move> legalMoves)
        {
            for (var directionOffsetIndex = 0; directionOffsetIndex < 8; directionOffsetIndex++)
            {
                var captureCount = 0;
                var currentSquare = square + MoveData.DirectionOffsets[directionOffsetIndex];
                if (Board.IsOutOfBounds(currentSquare)) continue;

                for (var timesMoved = 1; timesMoved < MoveData.SquaresToEdge[square][directionOffsetIndex]; timesMoved++)
                {
                    if (!IsOpponentPiece(currentSquare)) break;
                    captureCount++;
                    currentSquare += MoveData.DirectionOffsets[directionOffsetIndex];
                }

                if (!IsFriendlyPiece(currentSquare) || captureCount <= 0) continue;
                legalMoves.Add(new Move(square));
                break;
            }
        }

        private HashSet<Move> GetCaptureIndices(Move move)
        {
            var allCaptures = new HashSet<Move>();
            for (var directionOffsetIndex = 0; directionOffsetIndex < 8; directionOffsetIndex++)
            {
                var currentCaptures = new HashSet<Move>();
                var currentSquare = move.Index + MoveData.DirectionOffsets[directionOffsetIndex];
                if (Board.IsOutOfBounds(currentSquare)) continue;

                for (var timesMoved = 1; timesMoved < MoveData.SquaresToEdge[move.Index][directionOffsetIndex]; timesMoved++)
                {
                    if (!IsOpponentPiece(currentSquare)) break;
                    currentCaptures.Add(new Move(currentSquare));
                    currentSquare += MoveData.DirectionOffsets[directionOffsetIndex];
                }

                if (!IsFriendlyPiece(currentSquare) || currentCaptures.Count <= 0) continue;
                allCaptures.UnionWith(currentCaptures);
            }
            return allCaptures;
        }
        
    }
}