using System;
using UnityEngine;

namespace Othello.Core
{
    public abstract class Player : ICloneable
    {
        public event Action OnNoLegalMove;
        public event Action<Move> OnMoveChosen;
        
        public const byte BLACK = 0b01;
        public const byte WHITE = 0b10;
        
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
            Console.Log("----------------------------------------------------");
            OnNoLegalMove?.Invoke();
        }
        
        public abstract void NotifyTurnToMove();

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
    
}