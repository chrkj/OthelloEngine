using System;
using System.Collections.Generic;
using Othello.UI;
using Object = UnityEngine.Object;

namespace Othello.Core
{
    public abstract class Player
    {
        public event Action ONNoLegalMove;
        public event Action<int> ONMoveChosen;
        
        protected readonly int color;
        protected readonly Board board;
        protected readonly BoardUI boardUI;
        protected List<int> legalMoves;

        protected Player(Board board, int color)
        {
            this.board = board;
            this.color = color;
            boardUI = Object.FindObjectOfType<BoardUI>();
        }
        
        public abstract void Update();

        protected void ChooseMove(int move)
        {
            ONMoveChosen?.Invoke(move);
        }
        
        protected void NoLegalMove()
        {
            ONNoLegalMove?.Invoke();
        }
        
        // TODO: Calc legalmoves here
        public abstract void NotifyTurnToMove();
    }
}