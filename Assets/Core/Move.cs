using System;

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
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Move) obj);
        }

        public bool Equals(Move other)
        {
            return m_index == other.m_index;
        }

        public override int GetHashCode()
        {
            return m_index;
        }

        public static bool operator ==(Move left, Move right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Move left, Move right)
        {
            return !Equals(left, right);
        }
    }
}