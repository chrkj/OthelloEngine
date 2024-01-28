using System;
using Othello.UI;

namespace Othello.Core
{
    public class Move : IEquatable<Move>, IComparable<Move>
    {
        public int Index => m_index;

        private readonly int m_index;
        
        public static readonly int[] m_cellWeight = new int[64]
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
            m_index = index;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) 
                return false;
            if (ReferenceEquals(this, obj)) 
                return true;
            return obj.GetType() == GetType() && Equals((Move) obj);
        }

        public bool Equals(Move other)
        {
            return m_index == other.m_index;
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
            return m_index.GetHashCode();
        }

        public override string ToString()
        {
            var rank = ((m_index >> 3) + 1).ToString();
            var file = BoardUI.FileChars[m_index & 7].ToUpper();
            return file + rank;
        }

        public int CompareTo(Move other)
        {
            if (m_cellWeight[Index] < m_cellWeight[other.Index])
                return 1;
            if (m_cellWeight[Index] > m_cellWeight[other.Index])
                return -1;
            return 0; 
        }
    }
}