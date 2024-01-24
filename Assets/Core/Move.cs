using System;
using Othello.UI;

namespace Othello.Core
{
    public class Move : IEquatable<Move>
    {
        private readonly int m_index;
        public int Index => m_index;

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
    }
}