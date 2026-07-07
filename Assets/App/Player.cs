using System;
using UnityEngine;
using Othello.Core;
using Console = Othello.UI.Console;

namespace Othello.App
{
    public abstract class Player : ICloneable
    {
        public event Action OnNoLegalMove;
        public event Action<Move> OnMoveChosen;
        
        // Player identity shares the engine's piece color encoding (see Othello.Core.Piece),
        // derived here so the two can never diverge. Use Player.* when referring to a player,
        // Piece.* when referring to the contents of a square.
        public const byte NONE = Piece.EMPTY;
        public const byte BLACK = Piece.BLACK;
        public const byte WHITE = Piece.WHITE;
        
        protected readonly Board m_Board;

        protected Player(Board board)
        {
            m_Board = board;
        }
        
        public abstract void Update();

        protected void ChooseMove(Move move)
        {
            OnMoveChosen?.Invoke(move);
        }
        
        protected void NoLegalMove()
        {
            Console.Log("No legal moves for " + m_Board.GetCurrentPlayerAsString(), Color.red);
            OnNoLegalMove?.Invoke();
        }
        
        public abstract void NotifyTurnToMove();

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
    
}