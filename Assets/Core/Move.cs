using System.Collections.Generic;

namespace Othello.Core
{
    public class Move
    {
        public readonly int piece;
        public readonly int targetSquare;

        public Move(int targetSquare, int piece)
        {
            this.piece = piece;
            this.targetSquare = targetSquare;
        }

        public override bool Equals(object other)
        {
            if (other is Move move)
                return targetSquare == move.targetSquare;
            return false;
        }
        
        public override int GetHashCode()
        {
            return targetSquare.GetHashCode();
        }
        
    }
}