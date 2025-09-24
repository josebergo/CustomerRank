using Core.Model;

namespace Services.Services
{
    public interface ILeaderboardService
    {
        public decimal UpdateScore(long customerId, decimal scoreDelta);
        public IReadOnlyList<Customer> GetCustomersByRank(int start, int end);
        public IReadOnlyList<Customer> GetCustomersById(long customerId, int high = 0, int low = 0);
    }
}
