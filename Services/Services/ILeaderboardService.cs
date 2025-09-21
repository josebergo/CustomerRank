using Core.Model;

namespace Services.Services
{
    public interface ILeaderboardService
    {
        public decimal UpdateScore(long customerId, decimal scoreDelta);
        public List<Customer> GetCustomersByRank(int start, int end);
        public List<Customer> GetCustomersById(long customerId, int high = 0, int low = 0);
    }
}
