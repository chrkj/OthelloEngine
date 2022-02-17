using System.Collections.Generic;

namespace Othello.Core
{
    public class Move
    {
        public readonly int Piece;
        public readonly int TargetSquare;

        public Move(int targetSquare, int piece)
        {
            Piece = piece;
            TargetSquare = targetSquare;
        }

        public override bool Equals(object other)
        {
            if (other is Move move)
                return TargetSquare == move.TargetSquare;
            return false;
        }
        
        public override int GetHashCode()
        {
            return TargetSquare.GetHashCode();
        }
        
    }
}