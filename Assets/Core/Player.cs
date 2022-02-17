using System;

namespace Othello.Core
{
    public abstract class Player
    {
        public event Action ONNoLegalMove;
        public event Action<Move> ONMoveChosen;
        
        public abstract void Update();

        protected void ChooseMove(Move move)
        {
            ONMoveChosen?.Invoke(move);
        }
        
        protected void ChangePlayer()
        {
            ONNoLegalMove?.Invoke();
        }
        
        public abstract void NotifyTurnToMove();
    }
}