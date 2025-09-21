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

        [HttpGet("/leaderboard")]
        public ActionResult<List<Customer>> GetCustomersByRank([FromQuery] int start, [FromQuery] int end)
        {
            if (start < 1 || end < start)
                return BadRequest("Invalid rank range");

            var customers = _leaderboardService.GetCustomersByRank(start, end);
            return Ok(customers);
        }

        [HttpGet("/leaderboard/{customerId}")]
        public ActionResult<List<Customer>> GetCustomersById(long customerId, [FromQuery] int high = 0, [FromQuery] int low = 0)
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
