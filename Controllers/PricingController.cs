using Microsoft.AspNetCore.Mvc;
using API_DigiBook.Models;
using API_DigiBook.Interfaces.Services;
using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Strategy;
using API_DigiBook.Strategy.Strategies;
using API_DigiBook.Singleton;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PricingController : ControllerBase
    {
        private readonly ILogger<PricingController> _logger;
        private readonly LoggerService _systemLogger;
        private readonly IUserRepository _userRepository;

        public PricingController(ILogger<PricingController> logger, IUserRepository userRepository)
        {
            _logger = logger;
            _systemLogger = LoggerService.Instance;
            _userRepository = userRepository;
        }

        /// <summary>
        /// Calculate price using specified pricing strategy
        /// </summary>
        /// <remarks>
        /// Strategy Pattern Demo - Choose different pricing strategies:
        /// - "regular": Standard pricing, no discounts
        /// - "member": 10% member discount
        /// - "wholesale": Bulk discounts (5-25% based on quantity)
        /// - "vip": VIP pricing with 20-30% discount
        /// 
        /// Example request:
        /// 
        ///     POST /api/pricing/calculate
        ///     {
        ///       "basePrice": 100000,
        ///       "quantity": 15,
        ///       "strategy": "wholesale",
        ///       "productName": "Programming Book"
        ///     }
        /// </remarks>
        [HttpPost("calculate")]
        public async Task<IActionResult> CalculatePrice([FromBody] PricingRequest request)
        {
            try
            {
                // Validate input
                if (request.BasePrice <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Base price must be greater than 0"
                    });
                }

                if (request.Quantity <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Quantity must be greater than 0"
                    });
                }

                // Select strategy based on request
                IPricingStrategy strategy = GetStrategy(request.Strategy.ToLower());

                // Create context with selected strategy
                var pricingContext = new PricingContext(strategy);

                // Calculate price
                double finalPrice = pricingContext.ExecuteStrategy(request.BasePrice, request.Quantity);
                double originalTotal = request.BasePrice * request.Quantity;
                double savings = originalTotal - finalPrice;
                double savingsPercentage = (savings / originalTotal) * 100;

                // Log the calculation
                await _systemLogger.LogInfoAsync(
                    "CALCULATE_PRICE",
                    $"Calculated price using {strategy.GetStrategyName()} strategy: {finalPrice:N0} VND",
                    "Anonymous"
                );

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        productName = request.ProductName ?? "Product",
                        basePrice = request.BasePrice,
                        quantity = request.Quantity,
                        originalTotal = originalTotal,
                        finalPrice = finalPrice,
                        savings = savings,
                        savingsPercentage = Math.Round(savingsPercentage, 2),
                        strategy = new
                        {
                            name = pricingContext.GetCurrentStrategyName(),
                            description = pricingContext.GetCurrentStrategyDescription()
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating price");
                
                await _systemLogger.LogErrorAsync(
                    "CALCULATE_PRICE",
                    $"Error calculating price: {ex.Message}",
                    "Anonymous"
                );

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error calculating price",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Compare prices across all pricing strategies
        /// </summary>
        [HttpPost("compare")]
        public async Task<IActionResult> ComparePrices([FromBody] PricingRequest request)
        {
            try
            {
                if (request.BasePrice <= 0 || request.Quantity <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Base price and quantity must be greater than 0"
                    });
                }

                var strategies = new List<IPricingStrategy>
                {
                    new RegularPricingStrategy(),
                    new MemberPricingStrategy(),
                    new WholesalePricingStrategy(),
                    new VIPPricingStrategy()
                };

                var comparisons = strategies.Select(strategy =>
                {
                    var context = new PricingContext(strategy);
                    double finalPrice = context.ExecuteStrategy(request.BasePrice, request.Quantity);
                    double originalTotal = request.BasePrice * request.Quantity;
                    double savings = originalTotal - finalPrice;

                    return new
                    {
                        strategy = strategy.GetStrategyName(),
                        description = strategy.GetDescription(),
                        finalPrice = finalPrice,
                        savings = savings,
                        savingsPercentage = Math.Round((savings / originalTotal) * 100, 2)
                    };
                }).ToList();

                // Find best deal
                var bestDeal = comparisons.OrderBy(c => c.finalPrice).First();

                await _systemLogger.LogInfoAsync(
                    "COMPARE_PRICES",
                    $"Compared prices for {request.Quantity} items at {request.BasePrice:N0} VND each",
                    "Anonymous"
                );

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        productName = request.ProductName ?? "Product",
                        basePrice = request.BasePrice,
                        quantity = request.Quantity,
                        originalTotal = request.BasePrice * request.Quantity,
                        comparisons = comparisons,
                        bestDeal = new
                        {
                            strategy = bestDeal.strategy,
                            finalPrice = bestDeal.finalPrice,
                            totalSavings = bestDeal.savings
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing prices");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error comparing prices",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get all available pricing strategies
        /// </summary>
        [HttpGet("strategies")]
        public IActionResult GetStrategies()
        {
            var strategies = new List<IPricingStrategy>
            {
                new RegularPricingStrategy(),
                new MemberPricingStrategy(),
                new WholesalePricingStrategy(),
                new VIPPricingStrategy()
            };

            var result = strategies.Select(s => new
            {
                name = s.GetStrategyName(),
                description = s.GetDescription(),
                key = s.GetStrategyName().ToLower().Replace(" ", "")
            }).ToList();

            return Ok(new
            {
                success = true,
                count = result.Count,
                data = result
            });
        }

        /// <summary>
        /// Calculate price for user based on their membership tier (Strategy Pattern in action!)
        /// </summary>
        [HttpPost("calculate-for-user/{userId}")]
        public async Task<IActionResult> CalculatePriceForUser(string userId, [FromBody] PricingRequest request)
        {
            try
            {
                // Get user to determine membership tier
                var user = await _userRepository.GetByIdAsync(userId);

                if (user == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "User not found"
                    });
                }

                // Auto-select strategy based on user membership
                IPricingStrategy strategy = user.MembershipTier?.ToLower() switch
                {
                    "vip" => new VIPPricingStrategy(),
                    "member" => new MemberPricingStrategy(),
                    _ => new RegularPricingStrategy()
                };

                var pricingContext = new PricingContext(strategy);
                double finalPrice = pricingContext.ExecuteStrategy(request.BasePrice, request.Quantity);
                double originalTotal = request.BasePrice * request.Quantity;
                double savings = originalTotal - finalPrice;

                await _systemLogger.LogInfoAsync(
                    "USER_PRICING",
                    $"Calculated price for {user.Name} ({user.MembershipTier}): {finalPrice:N0} VND",
                    userId
                );

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        userId = userId,
                        userName = user.Name,
                        membershipTier = user.MembershipTier,
                        productName = request.ProductName ?? "Product",
                        basePrice = request.BasePrice,
                        quantity = request.Quantity,
                        originalTotal = originalTotal,
                        finalPrice = finalPrice,
                        savings = savings,
                        savingsPercentage = Math.Round((savings / originalTotal) * 100, 2),
                        strategy = new
                        {
                            name = pricingContext.GetCurrentStrategyName(),
                            description = pricingContext.GetCurrentStrategyDescription()
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating price for user {UserId}", userId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error calculating price",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get user membership info
        /// </summary>
        [HttpGet("membership/{userId}")]
        public async Task<IActionResult> GetMembershipInfo(string userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);

                if (user == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "User not found"
                    });
                }

                var tierInfo = user.MembershipTier?.ToLower() switch
                {
                    "vip" => new { name = "VIP", discount = "20-30%", color = "#FFD700", benefits = new[] { "20% base discount", "Extra 5% for 5+ items", "Extra 5% for 10+ items", "Priority support", "Early access to sales" } },
                    "member" => new { name = "Member", discount = "10%", color = "#C0C0C0", benefits = new[] { "10% discount on all orders", "Member-only promotions", "Free shipping on orders > 500k" } },
                    _ => new { name = "Regular", discount = "0%", color = "#CD7F32", benefits = new[] { "Standard pricing", "Access to promotions", "Earn points for upgrade" } }
                };

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        userId = userId,
                        userName = user.Name,
                        membershipTier = user.MembershipTier ?? "regular",
                        membershipExpiry = user.MembershipExpiry,
                        totalSpent = user.TotalSpent,
                        tierInfo = tierInfo
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting membership info for user {UserId}", userId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving membership info",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Helper method to get strategy based on string input
        /// </summary>
        private IPricingStrategy GetStrategy(string strategyName)
        {
            return strategyName switch
            {
                "member" => new MemberPricingStrategy(),
                "wholesale" => new WholesalePricingStrategy(),
                "vip" => new VIPPricingStrategy(),
                _ => new RegularPricingStrategy()
            };
        }
    }
}
