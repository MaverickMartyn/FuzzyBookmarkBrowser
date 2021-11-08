using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FuzzyBookmarkBrowser
{
    public static class LevenshteinComparer
    {
        public static int Difference(string a, string b)
        {
            if (a == null)
                a = String.Empty;
            if (b == null)
                b = String.Empty;
            if (a.Length == 0)
                return b.Length;
            if (b.Length == 0)
                return a.Length;
            if (a[0] == b[0])
                return Difference(a[1..], b[1..]);
            return 1 + new[]{
                Difference(a[1..], b),
                Difference(a, b[1..]),
                Difference(a[1..], b[1..])
            }.Min();
        }
    }
}
