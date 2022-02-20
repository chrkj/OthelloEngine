using System.Collections.Generic;

namespace Othello.Core
{
    public class Move
    {
        public readonly int piece;
        public readonly int targetSquare;
        public readonly HashSet<int> captures;

        public Move(int targetSquare, int piece, HashSet<int> captures)
        {
            this.piece = piece;
            this.captures = captures;
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