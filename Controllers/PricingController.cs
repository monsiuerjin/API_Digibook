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
                    "wholesale" => new WholesalePricingStrategy(),
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
                    "wholesale" => new { name = "Wholesale", discount = "15%", color = "#4169E1", benefits = new[] { "15% discount for bulk orders", "Best for business customers", "Volume-based pricing", "Dedicated account manager" } },
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

        // ===== PHASE 3: MARKETPLACE COMPARISON =====

        /// <summary>
        /// Compare book prices across multiple marketplaces
        /// </summary>
        /// <param name="bookId">Book ID to compare</param>
        [HttpGet("marketplace-compare/{bookId}")]
        public async Task<IActionResult> CompareMarketplacePrices(string bookId)
        {
            try
            {
                // In a real implementation, this would call external APIs
                // For demo purposes, we'll simulate marketplace data
                
                var rand = new Random();
                var basePrice = 100000 + rand.Next(50000); // Random base price

                var marketplaces = new[]
                {
                    new
                    {
                        platform = "DigiBook",
                        price = basePrice,
                        availability = "In Stock",
                        shippingFee = 0,
                        estimatedDelivery = "1-2 days",
                        rating = 4.8,
                        seller = "DigiBook Official",
                        isOwnPlatform = true
                    },
                    new
                    {
                        platform = "Tiki",
                        price = basePrice + rand.Next(-20000, 30000),
                        availability = "In Stock",
                        shippingFee = 15000,
                        estimatedDelivery = "2-3 days",
                        rating = 4.5,
                        seller = "Tiki Trading",
                        isOwnPlatform = false
                    },
                    new
                    {
                        platform = "Shopee",
                        price = basePrice + rand.Next(-15000, 25000),
                        availability = "In Stock",
                        shippingFee = 20000,
                        estimatedDelivery = "3-5 days",
                        rating = 4.3,
                        seller = "Shopee Mall",
                        isOwnPlatform = false
                    },
                    new
                    {
                        platform = "Fahasa",
                        price = basePrice + rand.Next(-10000, 40000),
                        availability = "Limited Stock",
                        shippingFee = 25000,
                        estimatedDelivery = "2-4 days",
                        rating = 4.6,
                        seller = "Fahasa Store",
                        isOwnPlatform = false
                    },
                    new
                    {
                        platform = "Lazada",
                        price = basePrice + rand.Next(-25000, 35000),
                        availability = "In Stock",
                        shippingFee = 18000,
                        estimatedDelivery = "3-5 days",
                        rating = 4.2,
                        seller = "Lazada Books",
                        isOwnPlatform = false
                    }
                };

                // Calculate total costs and find best deal
                var comparison = marketplaces.Select(m => new
                {
                    platform = m.platform,
                    price = m.price,
                    shippingFee = m.shippingFee,
                    totalCost = m.price + m.shippingFee,
                    availability = m.availability,
                    estimatedDelivery = m.estimatedDelivery,
                    rating = m.rating,
                    seller = m.seller,
                    isOwnPlatform = m.isOwnPlatform,
                    priceDifference = m.price - basePrice,
                    savingsVsOurs = basePrice - (m.price + m.shippingFee)
                }).OrderBy(x => x.totalCost).ToList();

                var bestDeal = comparison.First();
                var ourPlatform = comparison.First(x => x.isOwnPlatform);

                await _systemLogger.LogInfoAsync(
                    "MARKETPLACE_COMPARE",
                    $"Compared prices for book {bookId} across {marketplaces.Length} marketplaces",
                    "Anonymous"
                );

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        bookId = bookId,
                        comparedAt = DateTime.Now,
                        marketplaceCount = marketplaces.Length,
                        comparisons = comparison,
                        bestDeal = new
                        {
                            platform = bestDeal.platform,
                            totalCost = bestDeal.totalCost,
                            savings = ourPlatform.totalCost - bestDeal.totalCost
                        },
                        ourPrice = new
                        {
                            platform = ourPlatform.platform,
                            price = ourPlatform.price,
                            totalCost = ourPlatform.totalCost,
                            rank = comparison.FindIndex(x => x.isOwnPlatform) + 1
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing marketplace prices for book {BookId}", bookId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error comparing marketplace prices",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get best deals - books with largest price difference between our platform and competitors
        /// </summary>
        [HttpGet("best-deals")]
        public async Task<IActionResult> GetBestDeals([FromQuery] int limit = 10)
        {
            try
            {
                // In real implementation, this would query actual marketplace data
                // For demo, we'll generate sample data
                
                var rand = new Random();
                var deals = new List<object>();

                for (int i = 0; i < Math.Min(limit, 20); i++)
                {
                    var bookId = $"book-{Guid.NewGuid().ToString().Substring(0, 8)}";
                    var ourPrice = 80000 + rand.Next(120000);
                    var competitorAvg = ourPrice + rand.Next(-40000, 60000);
                    var savings = competitorAvg - ourPrice;
                    var savingsPercent = (savings / (double)competitorAvg) * 100;

                    deals.Add(new
                    {
                        bookId = bookId,
                        bookTitle = $"Sample Book {i + 1}",
                        ourPrice = ourPrice,
                        competitorAveragePrice = competitorAvg,
                        savings = savings,
                        savingsPercentage = Math.Round(savingsPercent, 2),
                        cheapestCompetitor = new
                        {
                            platform = new[] { "Tiki", "Shopee", "Fahasa", "Lazada" }[rand.Next(4)],
                            price = competitorAvg - rand.Next(0, 20000)
                        },
                        mostExpensiveCompetitor = new
                        {
                            platform = new[] { "Tiki", "Shopee", "Fahasa", "Lazada" }[rand.Next(4)],
                            price = competitorAvg + rand.Next(0, 30000)
                        },
                        dealQuality = savingsPercent > 15 ? "Excellent" : savingsPercent > 5 ? "Good" : "Fair"
                    });
                }

                // Sort by savings percentage (best deals first)
                var sortedDeals = deals.OrderByDescending(d => ((dynamic)d).savingsPercentage).ToList();

                await _systemLogger.LogInfoAsync(
                    "BEST_DEALS",
                    $"Retrieved {sortedDeals.Count} best deals",
                    "Anonymous"
                );

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        generatedAt = DateTime.Now,
                        count = sortedDeals.Count,
                        deals = sortedDeals,
                        summary = new
                        {
                            excellentDeals = sortedDeals.Count(d => ((dynamic)d).dealQuality == "Excellent"),
                            goodDeals = sortedDeals.Count(d => ((dynamic)d).dealQuality == "Good"),
                            fairDeals = sortedDeals.Count(d => ((dynamic)d).dealQuality == "Fair")
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting best deals");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving best deals",
                    error = ex.Message
                });
            }
        }
    }
}
