using System;

namespace Othello.Utility
{
    public static class SpanExtensions
    {
        public static void Sort<T>(this Span<T> span) where T : IComparable<T>
        {
            if (span.Length > 1)
                QuickSort(span);
        }

        private static void QuickSort<T>(Span<T> span) where T : IComparable<T>
        {
            if (span.Length <= 1)
                return;

            T pivot = span[span.Length / 2];
            int i = 0;
            int j = span.Length - 1;

            while (i <= j)
            {
                while (span[i].CompareTo(pivot) < 0)
                    i++;

                while (span[j].CompareTo(pivot) > 0)
                    j--;

                if (i <= j)
                {
                    (span[i], span[j]) = (span[j], span[i]);
                    i++;
                    j--;
                }
            }

            if (j > 0)
                QuickSort(span.Slice(0, j + 1));

            if (i < span.Length)
                QuickSort(span.Slice(i, span.Length - i));
        }
    }
}