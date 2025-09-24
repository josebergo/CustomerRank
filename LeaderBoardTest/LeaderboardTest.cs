using Core.Model;
using CustomerRank.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Services.Services;
using System.Diagnostics;

namespace LeaderBoardTest
{
    [TestClass]
    public sealed class LeaderboardTest
    {

        private Mock<ILeaderboardService> _mockLeaderboardService;
        private LeaderboardController _controller;

        [TestInitialize]
        public void Setup()
        {

            _mockLeaderboardService = new Mock<ILeaderboardService>();
            _controller = new LeaderboardController(_mockLeaderboardService.Object);
        }

        [TestMethod]
        public void UpdateScore_500K_Insertions_PerformanceTest()
        {
            // Arrange
            const int insertionCount = 5_000_000;
            var random = new Random(42); // Fixed seed for reproducible results
            var stopwatch = new Stopwatch();

            _mockLeaderboardService
            .Setup(s => s.UpdateScore(It.IsAny<long>(), It.IsAny<decimal>()))
            .Returns<long, decimal>((id, score) => score);

            // Act
            stopwatch.Start();

            for (int i = 1; i <= insertionCount; i++)
            {
                long customerId = i;
                // Random score between -1000 and 1000
                decimal score = (decimal)(random.NextDouble() * 2000 - 1000);

                var result = _controller.UpdateScore(customerId, score);

                if (i % 10000 == 0)
                {
                    Assert.IsInstanceOfType(result, typeof(ActionResult<decimal>));
                    var actionResult = (ActionResult<decimal>)result;

                    Console.WriteLine($"Processed {i:N0} insertions...");
                }
            }

            stopwatch.Stop();

            // Assert
            Console.WriteLine($"100K insertions completed in {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Average time per insertion: {(double)stopwatch.ElapsedMilliseconds / insertionCount:F4} ms");
            Console.WriteLine($"Throughput: {insertionCount / (stopwatch.ElapsedMilliseconds / 1000.0):F0} insertions/second");

            _mockLeaderboardService.Verify(s => s.UpdateScore(It.IsAny<long>(), It.IsAny<decimal>()),
          Times.Exactly(insertionCount));

            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 30000,
                $"100K insertions took too long: {stopwatch.ElapsedMilliseconds} ms");
        }

        [TestMethod]
        public void UpdateScore_ValidInput_ReturnsOkResult()
        {
            // Arrange
            long customerId = 1;
            decimal score = 100.5m;
            decimal expectedScore = 100.5m;

            _mockLeaderboardService
                .Setup(s => s.UpdateScore(customerId, score))
                .Returns(expectedScore);

            // Act
            var result = _controller.UpdateScore(customerId, score);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ActionResult<decimal>));
            var actionResult = (ActionResult<decimal>)result;
            Assert.IsInstanceOfType(actionResult.Result, typeof(OkObjectResult));

            var okResult = (OkObjectResult)actionResult.Result;
            Assert.AreEqual(expectedScore, okResult.Value);

            _mockLeaderboardService.Verify(s => s.UpdateScore(customerId, score), Times.Once);
        }

        [TestMethod]
        public void UpdateScore_InvalidScore_ReturnsBadRequest()
        {
            // Arrange
            long customerId = 1;
            decimal invalidScore = 1001; // Above valid range

            // Act
            var result = _controller.UpdateScore(customerId, invalidScore);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ActionResult<decimal>));
            var actionResult = (ActionResult<decimal>)result;
            Assert.IsInstanceOfType(actionResult.Result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public void UpdateScore_InvalidCustomerId_ReturnsBadRequest()
        {
            // Arrange
            long invalidCustomerId = 0; // Invalid customer ID
            decimal score = 100;

            // Act
            var result = _controller.UpdateScore(invalidCustomerId, score);
            Assert.IsInstanceOfType(result, typeof(ActionResult<decimal>));
            var actionResult = (ActionResult<decimal>)result;
            Assert.IsInstanceOfType(actionResult.Result, typeof(BadRequestObjectResult));
        }


        [TestMethod]
        public void GetCustomersByRank_ValidRange_ReturnsOkResult()
        {
            // Arrange
            int start = 1;
            int end = 10;
            var expectedCustomers = new List<Customer>
            {
                new Customer { CustomerId = 1, Score = 1000, Rank = 1 },
                new Customer { CustomerId = 2, Score = 900, Rank = 2 },
                new Customer { CustomerId = 3, Score = 800, Rank = 3 }
            };

            _mockLeaderboardService
                .Setup(s => s.GetCustomersByRank(start, end))
                .Returns(expectedCustomers);

            // Act
            var result = _controller.GetCustomersByRank(start, end);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ActionResult<List<Customer>>));
            var actionResult = (ActionResult<List<Customer>>)result;
            Assert.IsInstanceOfType(actionResult.Result, typeof(OkObjectResult));

            var okResult = (OkObjectResult)actionResult.Result;
            var customers = (List<Customer>)okResult.Value;

            Assert.AreEqual(3, customers.Count);
            Assert.AreEqual(1, customers[0].CustomerId);
            Assert.AreEqual(1000, customers[0].Score);
            Assert.AreEqual(1, customers[0].Rank);

            _mockLeaderboardService.Verify(s => s.GetCustomersByRank(start, end), Times.Once);
        }

        #region GetCustomersByRank Tests

        [TestMethod]
        public void GetCustomersByRank_InvalidStartRank_ReturnsBadRequest()
        {
            // Arrange
            int invalidStart = 0; // Invalid start rank
            int end = 10;

            // Act
            var result = _controller.GetCustomersByRank(invalidStart, end);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ActionResult<List<Customer>>));
            var actionResult = (ActionResult<List<Customer>>)result;
            Assert.IsInstanceOfType(actionResult.Result, typeof(BadRequestObjectResult));

            var badRequestResult = (BadRequestObjectResult)actionResult.Result;
            Assert.AreEqual("Invalid rank range", badRequestResult.Value);

            _mockLeaderboardService.Verify(s => s.GetCustomersByRank(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        #endregion

        #region GetCustomersById Tests

        [TestMethod]
        public void GetCustomersById_ValidCustomerId_ReturnsOkResult()
        {
            // Arrange
            long customerId = 123;
            int high = 2;
            int low = 2;
            var expectedCustomers = new List<Customer>
            {
                new Customer { CustomerId = 121, Score = 1200, Rank = 3 },
                new Customer { CustomerId = 122, Score = 1100, Rank = 4 },
                new Customer { CustomerId = 123, Score = 1000, Rank = 5 }, // Target customer
                new Customer { CustomerId = 124, Score = 900, Rank = 6 },
                new Customer { CustomerId = 125, Score = 800, Rank = 7 }
            }.AsReadOnly();

            _mockLeaderboardService
                .Setup(s => s.GetCustomersById(customerId, high, low))
                .Returns(expectedCustomers);

            // Act
            var result = _controller.GetCustomersById(customerId, high, low);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ActionResult<IReadOnlyList<Customer>>));
            var actionResult = (ActionResult<IReadOnlyList<Customer>>)result;
            Assert.IsInstanceOfType(actionResult.Result, typeof(OkObjectResult));

            var okResult = (OkObjectResult)actionResult.Result;
            var customers = (IReadOnlyList<Customer>)okResult.Value;

            Assert.AreEqual(5, customers.Count);
            Assert.AreEqual(123, customers[2].CustomerId); // Target customer should be in the middle
            Assert.AreEqual(1000, customers[2].Score);
            Assert.AreEqual(5, customers[2].Rank);

            _mockLeaderboardService.Verify(s => s.GetCustomersById(customerId, high, low), Times.Once);
        }

        [TestMethod]
        public void GetCustomersById_InvalidCustomerId_ReturnsBadRequest()
        {
            // Arrange
            long invalidCustomerId = 0; // Invalid customer ID
            int high = 2000;
            int low = 10000;

            // Act
            var result = _controller.GetCustomersById(invalidCustomerId, high, low);

            // Assert
            Assert.IsInstanceOfType(result, typeof(ActionResult<IReadOnlyList<Customer>>));
            var actionResult = (ActionResult<IReadOnlyList<Customer>>)result;
            Assert.IsInstanceOfType(actionResult.Result, typeof(BadRequestObjectResult));

            var badRequestResult = (BadRequestObjectResult)actionResult.Result;
            Assert.AreEqual("CustomerId must be positive", badRequestResult.Value);

            _mockLeaderboardService.Verify(s => s.GetCustomersById(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        #endregion



    }
}