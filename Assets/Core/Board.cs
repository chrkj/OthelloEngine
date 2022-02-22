using System;
using System.Linq;
using System.Collections.Generic;

namespace Othello.Core
{
    public class Board
    {
        private int m_lastMove;
        private bool m_isWhiteToMove;
        private int[] m_board = new int[64];

        public Board(int playerToStart)
        {
            m_lastMove = -1;
            m_isWhiteToMove = playerToStart == Piece.White;
        }
        
        private Board() { }

        public Board Copy()
        {
            var copy = new Board();
            copy.m_isWhiteToMove = m_isWhiteToMove;
            Array.Copy(m_board, copy.m_board, m_board.Length);
            return copy;
        }

        public void ResetBoard(int playerToStart)
        {
            m_lastMove = -1;
            m_board = new int[64];
            m_isWhiteToMove = playerToStart == Piece.White;
        }
        
        public void LoadStartPosition()
        {
            m_board[27] = Piece.Black;
            m_board[36] = Piece.Black;
            m_board[28] = Piece.White;
            m_board[35] = Piece.White;
        }
        
        public static int GetBoardIndex(int file, int rank)
        {
            return rank * 8 + file;
        }

        public int GetPieceColor(int file, int rank)
        {
            return m_board[GetBoardIndex(file, rank)];
        }

        public int GetCurrentPlayer()
        {
            return m_isWhiteToMove ? Piece.White : Piece.Black;
        }

        public int GetCurrentOpponent()
        {
            return m_isWhiteToMove ? Piece.Black : Piece.White;
        }

        public int GetLastMove()
        {
            return m_lastMove;
        }

        public List<int> GetEmptySquares()
        {
            var emptySquares = new List<int>();
            for (int i = 0; i < m_board.Length; i++)
                if (m_board[i] == Piece.Empty) emptySquares.Add(i);
            return emptySquares;
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
            return m_board[index] == GetCurrentOpponent();
        }
        
        public bool IsFriendlyPiece(int index)
        {
            return m_board[index] == GetCurrentPlayer();
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
            foreach (var square in m_board)
                if (square == player) count++;
            return count;
        }
        
        public int GetBoardState()
        {
            if (!IsTerminalBoardState(this)) return -1;
            return GetWinner();
        }
        
        public int GetWinner()
        {
            var playerPieceCount = GetPieceCount(GetCurrentPlayer());
            var opponentPieceCount = GetPieceCount(GetCurrentOpponent());
            if (playerPieceCount == opponentPieceCount) return 0;
            return playerPieceCount > opponentPieceCount ? GetCurrentPlayer() : GetCurrentOpponent();
        }

        public bool IsTerminalBoardState(Board board)
        {
            var legalMovesCurrentPlayer = MoveGenerator.GenerateLegalMoves(board).Count;
            ChangePlayer();
            var legalMovesCurrentOpponent = MoveGenerator.GenerateLegalMoves(board).Count;
            ChangePlayer();
            return legalMovesCurrentPlayer == 0 & legalMovesCurrentOpponent == 0;
        }

        public void SetStartingPlayer(int player)
        {
            m_isWhiteToMove = player == Piece.White; 
        }

        public void MakeMove(int move, HashSet<int> captures)
        {
            m_lastMove = move;
            m_board[move] = GetCurrentPlayer();
            foreach (var capture in captures)
                m_board[capture] = GetCurrentPlayer();
        }
        
        public string GetPieceCountAsString(int player)
        {
            var count = GetPieceCount(player);
            return count.ToString();
        }
        
        public override bool Equals(object board)
        {
            var cast = (Board)board;
            return cast != null && cast.m_board.SequenceEqual(m_board);
        }
        
    }
}