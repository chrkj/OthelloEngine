using System;

namespace Othello.Core
{
    public struct Move : IEquatable<Move>, IComparable<Move>
    {
        public int Index { get; }
        public static readonly Move NULLMOVE = new Move(-1);

        public static readonly int[] s_CellWeight = 
        {
            30,  -12, 0,  -1, -1, 0,  -12, 30,  
            -12, -15, -3, -3, -3, -3, -15, -12,
            0,   -3,  0,  -1, -1, 0,  -3,  0,   
            -1,  -3,  -1, -1, -1, -1, -3,  -1,
            -1,  -3,  -1, -1, -1, -1, -3,  -1,  
            0,   -3,  0,  -1, -1, 0,  -3,  0,
            -12, -15, -3, -3, -3, -3, -15, -12, 
            30,  -12, 0,  -1, -1, 0,  -12, 30
        };

        public Move(int index)
        {
            Index = index;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) 
                return false;
            return obj.GetType() == GetType() && Equals((Move) obj);
        }

        public bool Equals(Move other)
        {
            return Index == other.Index;
        }

        public static bool operator ==(Move left, Move right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Move left, Move right)
        {
            return !Equals(left, right);
        }

        public override int GetHashCode()
        {
            return Index.GetHashCode();
        }

        public override string ToString()
        {
            if (Index < 0)
                return "null";
            var rank = (Index >> 3) + 1;
            var file = (char)('A' + (Index & 7));
            return file + rank.ToString();
        }

        public int CompareTo(Move other)
        {
            if (s_CellWeight[Index] < s_CellWeight[other.Index])
                return 1;
            if (s_CellWeight[Index] > s_CellWeight[other.Index])
                return -1;
            return 0; 
        }
    }
}