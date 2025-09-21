using Core.Model;

namespace Services.Services;

public class LeaderboardService : ILeaderboardService
{
    // SortedSet for O(log n) operations on score-based ordering
    private readonly SortedSet<(decimal Score, long CustomerId, Customer Customer)> _scoreSet;
    // Dictionary for O(1) customer lookup
    private readonly Dictionary<long, Customer> _customers;

    public LeaderboardService()
    {
        _scoreSet = new SortedSet<(decimal Score, long CustomerId, Customer Customer)>(
            Comparer<(decimal Score, long CustomerId, Customer Customer)>.Create((a, b) =>
            {
                var scoreCompare = b.Score.CompareTo(a.Score); // Descending score
                return scoreCompare != 0 ? scoreCompare : a.CustomerId.CompareTo(b.CustomerId); // Ascending ID for same score
            }));
        _customers = new Dictionary<long, Customer>();
    }


    public decimal UpdateScore(long customerId, decimal scoreDelta)
    {
        lock (_scoreSet) // Thread-safety for concurrent access
        {
            if (!_customers.TryGetValue(customerId, out var customer))
            {
                customer = new Customer { CustomerId = customerId, Score = 0 };
                _customers[customerId] = customer;
            }

            // Remove old entry from SortedSet if exists
            if (customer.Score > 0)
            {
                _scoreSet.Remove((customer.Score, customerId, customer));
            }

            // Update score
            customer.Score = Math.Max(0, customer.Score + scoreDelta);

            // Add back to SortedSet if score > 0
            if (customer.Score > 0)
            {
                _scoreSet.Add((customer.Score, customerId, customer));
                // Update ranks
                UpdateRanks();
            }
            else
            {
                customer.Rank = 0; // Not in leaderboard if score <= 0
            }

            return customer.Score;
        }
    }

    private void UpdateRanks()
    {
        int rank = 1;
        foreach (var entry in _scoreSet)
        {
            entry.Customer.Rank = rank++;
        }
    }

    public List<Customer> GetCustomersByRank(int start, int end)
    {
        lock (_scoreSet)
        {
            if (start < 1 || end < start || _scoreSet.Count == 0)
                return new List<Customer>();

            // Directly access SortedSet elements by index range
            return _scoreSet
                .Skip(start - 1)
                .Take(end - start + 1)
                .Select(x => x.Customer)
                .ToList();
        }
    }

    public List<Customer> GetCustomersById(long customerId, int high = 0, int low = 0)
    {
        lock (_scoreSet)
        {
            if (!_customers.TryGetValue(customerId, out var customer) || customer.Rank == 0)
                return new List<Customer>();

            var result = new List<Customer>();
            int targetRank = customer.Rank;

            // Get higher ranked customers
            if (high > 0)
            {
                result.AddRange(_scoreSet
                    .Skip(Math.Max(0, targetRank - high - 1))
                    .Take(Math.Min(high, targetRank - 1))
                    .Select(x => x.Customer));
            }

            // Add target customer
            result.Add(customer);

            // Get lower ranked customers
            if (low > 0)
            {
                result.AddRange(_scoreSet
                    .Skip(targetRank)
                    .Take(low)
                    .Select(x => x.Customer));
            }

            return result;
        }
    }
}
