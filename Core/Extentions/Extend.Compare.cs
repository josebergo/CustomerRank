using Core.Model;

namespace Core.Extentions
{
    public static class Extend
    {
        public static IComparer<(decimal Score, long CustomerId, Customer Customer)> CreateScoreComparer()
        {
            return Comparer<(decimal Score, long CustomerId, Customer Customer)>.Create((a, b) =>
            {
                var scoreCompare = b.Score.CompareTo(a.Score); // Descending score
                return scoreCompare != 0 ? scoreCompare : a.CustomerId.CompareTo(b.CustomerId); // Ascending ID for same score
            });
        }
    }
}
