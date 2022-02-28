using System;
using Othello.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Othello.Core
{
    public abstract class Player : ICloneable
    {
        public event Action OnNoLegalMove;
        public event Action<Move> OnMoveChosen;
        
        protected readonly Board m_Board;
        protected readonly BoardUI m_BoardUI;

        protected Player(Board board)
        {
            m_Board = board;
            m_BoardUI = Object.FindObjectOfType<BoardUI>();
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