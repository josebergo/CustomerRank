using Core.Model;
using Microsoft.AspNetCore.Mvc;
using Services.Services;

namespace CustomerRank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaderboardController : ControllerBase
    {
        private readonly ILeaderboardService _leaderboardService;

        public LeaderboardController(ILeaderboardService leaderboardService)
        {
            _leaderboardService = leaderboardService;
        }

        [HttpPost("/customer/{customerId}/score/{score}")]
        public ActionResult<decimal> UpdateScore(long customerId, decimal score)
        {
            if (score < -1000 || score > 1000)
                return BadRequest("Score must be between -1000 and 1000");

            if (customerId <= 0)
                return BadRequest("CustomerId must be positive");

            var newScore = _leaderboardService.UpdateScore(customerId, score);
            return Ok(newScore);
        }

        [HttpPost("/customer/test")]
        public ActionResult<decimal> UpdateScore1()
        {
            const int insertionCount = 5_000_000;
            var random = new Random(42);

            var stardate = DateTime.Now;

            for (int i = 1; i <= insertionCount; i++)
            {
                long customerId = i;
                decimal score = (decimal)(random.NextDouble() * 2000 - 1000);

                var result = _leaderboardService.UpdateScore(customerId, score);

                if (i % 10000 == 0)
                {
                    Console.WriteLine($"Processed {i:N0} insertions...");
                }
            }

            TimeSpan timeSpan = DateTime.Now - stardate;
            Console.WriteLine($"Total time: {timeSpan.TotalSeconds:N2} seconds");
 
            return Ok();
        }

        [HttpGet("/leaderboard")]
        public ActionResult<List<Customer>> GetCustomersByRank([FromQuery] int start, [FromQuery] int end)
        {
            if (start < 1 || end < start)
                return BadRequest("Invalid rank range");

            var customers = _leaderboardService.GetCustomersByRank(start, end);
            return Ok(customers);
        }        

        [HttpGet("/leaderboard/{customerId}")]
        public ActionResult<IReadOnlyList<Customer>> GetCustomersById(long customerId, [FromQuery] int high = 0, [FromQuery] int low = 0)
        {
            if (customerId <= 0)
                return BadRequest("CustomerId must be positive");

            if (high < 0 || low < 0)
                return BadRequest("High and low parameters must be non-negative");

            var customers = _leaderboardService.GetCustomersById(customerId, high, low);
            return Ok(customers);
        }
         
    }
}
