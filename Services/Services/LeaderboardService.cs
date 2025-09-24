using Core.Extentions;
using Core.Model;
using System.Collections.Concurrent;

namespace Services.Services
{
    public class LeaderboardService : ILeaderboardService
    {
        private readonly ConcurrentDictionary<long, Customer> _customers = new();
        private readonly object _rebuildLock = new object();
        private volatile List<Customer> _rankedCustomers;
        private readonly Timer _rebuildTimer;

        public LeaderboardService()
        {
            _rankedCustomers = new List<Customer>();
            // set 100ms
            _rebuildTimer = new Timer(_ => RebuildLeaderboard(), null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
        }

        public decimal UpdateScore(long customerId, decimal scoreDelta)
        {
            var customer = _customers.AddOrUpdate(customerId,
                new Customer { CustomerId = customerId, Score = Math.Max(0, scoreDelta) },
                (id, existing) =>
                {
                    existing.Score = Math.Max(0, existing.Score + scoreDelta);
                    return existing;
                });

            return customer.Score;
        }

        private void RebuildLeaderboard()
        {
            //non-blocking
            if (!Monitor.TryEnter(_rebuildLock))
                return; 

            try
            {
                var newRankedList = new List<Customer>();
                int rank = 1;
                foreach (var customer in _customers.Values.Where(c => c.Score > 0).OrderByDescending(c => c.Score).ThenBy(c => c.CustomerId))
                {
                    customer.Rank = rank++;
                    newRankedList.Add(customer);
                }

                //atom replace
                _rankedCustomers = newRankedList;
            }
            finally
            {
                Monitor.Exit(_rebuildLock);
            }
        }

        public IReadOnlyList<Customer> GetCustomersByRank(int start, int end)
        {
            // get current snapshot
            var currentRankedList = _rankedCustomers;

            if (start < 1 || end < start || currentRankedList.Count == 0)
                return Array.Empty<Customer>();
 

            // boundary checking
            int actualStart = Math.Max(0, start - 1);
            int actualEnd = Math.Min(currentRankedList.Count - 1, end - 1);
            int count = actualEnd - actualStart + 1;

            if (count <= 0)
                return Array.Empty<Customer>();

            // the GetRange of List is O(n) than fastest SortedSet.Skip
            return currentRankedList.GetRange(actualStart, count);
        }

 


        public IReadOnlyList<Customer> GetCustomersById(long customerId, int high = 0, int low = 0)
        {
            // Get current snapshot
            var currentRankedList = _rankedCustomers; 

            if (!_customers.TryGetValue(customerId, out var customer) || customer.Rank == 0)
                return Array.Empty<Customer>();

            int targetRank = customer.Rank;
            // as 0 index
            int targetIndex = targetRank - 1; 

            // expected capacity
            int totalCount = Math.Min(high, targetRank - 1) + 1 + Math.Min(low, currentRankedList.Count - targetRank);
            var result = new List<Customer>(totalCount);

            // use index access O(1) 
            // get high
            if (high > 0 && targetRank > 1)
            {
                int startIndex = Math.Max(0, targetIndex - high);
                int endIndex = targetIndex - 1;

                for (int i = startIndex; i <= endIndex; i++)
                {
                    result.Add(currentRankedList[i]);
                }
            }

            // add self
            result.Add(customer);

            // get low
            if (low > 0 && targetIndex + 1 < currentRankedList.Count)
            {
                int startIndex = targetIndex + 1;
                int endIndex = Math.Min(currentRankedList.Count - 1, targetIndex + low);

                for (int i = startIndex; i <= endIndex; i++)
                {
                    result.Add(currentRankedList[i]);
                }
            }

            return result;
        }
    }
}
