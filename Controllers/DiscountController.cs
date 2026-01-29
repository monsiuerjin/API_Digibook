using Microsoft.AspNetCore.Mvc;
using API_DigiBook.Models;
using API_DigiBook.Interfaces.Services;
using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Decorator;
using API_DigiBook.Decorator.Decorators;
using API_DigiBook.Singleton;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiscountController : ControllerBase
    {
        private readonly ILogger<DiscountController> _logger;
        private readonly LoggerService _systemLogger;
        private readonly IUserRepository _userRepository;
        private readonly ICouponRepository _couponRepository;

        public DiscountController(
            ILogger<DiscountController> logger,
            IUserRepository userRepository,
            ICouponRepository couponRepository)
        {
            _logger = logger;
            _systemLogger = LoggerService.Instance;
            _userRepository = userRepository;
            _couponRepository = couponRepository;
        }

        /// <summary>
        /// Calculate price with multiple stacked discounts using Decorator Pattern
        /// </summary>
        [HttpPost("calculate")]
        public async Task<IActionResult> CalculateDiscount([FromBody] DiscountRequest request)
        {
            try
            {
                if (request.BasePrice <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Base price must be greater than 0"
                    });
                }

                // Start with base price
                IPriceCalculator calculator = new BasePriceCalculator(
                    request.BasePrice, 
                    request.ItemName
                );

                var appliedDiscounts = new List<string>();

                // Apply each discount decorator
                foreach (var discount in request.Discounts)
                {
                    calculator = ApplyDiscount(calculator, discount, request.Quantity);
                    appliedDiscounts.Add($"{discount.Type}: {discount.Reason ?? discount.Type}");
                }

                // Calculate final price
                var originalPrice = request.BasePrice;
                var finalPrice = calculator.Calculate();
                var totalDiscount = originalPrice - finalPrice;
                var discountPercentage = (totalDiscount / originalPrice) * 100;

                var response = new DiscountResponse
                {
                    OriginalPrice = originalPrice,
                    FinalPrice = finalPrice,
                    TotalDiscount = totalDiscount,
                    DiscountPercentage = Math.Round(discountPercentage, 2),
                    Description = calculator.GetDescription(),
                    AppliedDiscounts = appliedDiscounts
                };

                // Log discount calculation
                await _systemLogger.LogSuccessAsync(
                    "CALCULATE_DISCOUNT",
                    $"Calculated discount: {originalPrice:N0} -> {finalPrice:N0} VND " +
                    $"({appliedDiscounts.Count} discounts applied)",
                    "Anonymous"
                );

                return Ok(new
                {
                    success = true,
                    data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating discount");
                
                await _systemLogger.LogErrorAsync(
                    "CALCULATE_DISCOUNT",
                    $"Error: {ex.Message}",
                    "Anonymous"
                );

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error calculating discount",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Quick discount calculation with percentage
        /// </summary>
        [HttpGet("quick")]
        public async Task<IActionResult> QuickDiscount(
            [FromQuery] double price,
            [FromQuery] double percentage)
        {
            try
            {
                IPriceCalculator calculator = new BasePriceCalculator(price, "Item");
                calculator = new PercentageDiscountDecorator(calculator, percentage, "Quick Discount");

                var finalPrice = calculator.Calculate();
                var discount = price - finalPrice;

                await _systemLogger.LogInfoAsync(
                    "QUICK_DISCOUNT",
                    $"Quick discount: {price:N0} -> {finalPrice:N0} VND ({percentage}%)",
                    "Anonymous"
                );

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        originalPrice = price,
                        finalPrice = finalPrice,
                        discount = discount,
                        percentage = percentage,
                        description = calculator.GetDescription()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quick discount");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error calculating discount",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Example: Black Friday Sale
        /// </summary>
        [HttpPost("black-friday")]
        public async Task<IActionResult> BlackFridaySale([FromBody] SimpleDiscountRequest request)
        {
            try
            {
                IPriceCalculator calculator = new BasePriceCalculator(request.Price, request.ItemName);
                
                // Black Friday: 30% off
                calculator = new PercentageDiscountDecorator(calculator, 30, "Black Friday Sale");
                
                // Additional member discount if provided
                if (!string.IsNullOrEmpty(request.MembershipTier))
                {
                    calculator = new MembershipDiscountDecorator(calculator, request.MembershipTier);
                }
                
                // Bulk discount if buying multiple
                if (request.Quantity > 1)
                {
                    calculator = new BulkPurchaseDiscountDecorator(calculator, request.Quantity);
                }

                var finalPrice = calculator.Calculate();

                await _systemLogger.LogSuccessAsync(
                    "BLACK_FRIDAY_SALE",
                    $"Black Friday: {request.Price:N0} -> {finalPrice:N0} VND",
                    request.MembershipTier ?? "Anonymous"
                );

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        originalPrice = request.Price,
                        finalPrice = finalPrice,
                        discount = request.Price - finalPrice,
                        description = calculator.GetDescription()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Black Friday sale");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error calculating discount",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Validate and apply coupon code
        /// </summary>
        [HttpPost("apply-coupon")]
        public async Task<IActionResult> ApplyCoupon([FromBody] CouponRequest request)
        {
            try
            {
                // Simulate coupon validation (in real app, check database)
                var couponDiscounts = new Dictionary<string, (double value, bool isPercentage)>
                {
                    { "SAVE10", (10, true) },
                    { "SAVE20", (20, true) },
                    { "NEWYEAR", (50000, false) },
                    { "WELCOME", (15, true) }
                };

                if (!couponDiscounts.ContainsKey(request.CouponCode.ToUpper()))
                {
                    await _systemLogger.LogWarningAsync(
                        "INVALID_COUPON",
                        $"Invalid coupon code: {request.CouponCode}",
                        "Anonymous"
                    );

                    return BadRequest(new
                    {
                        success = false,
                        message = $"Invalid coupon code: {request.CouponCode}"
                    });
                }

                var (value, isPercentage) = couponDiscounts[request.CouponCode.ToUpper()];

                IPriceCalculator calculator = new BasePriceCalculator(request.Price, request.ItemName);
                calculator = new CouponDiscountDecorator(calculator, request.CouponCode, value, isPercentage);

                var finalPrice = calculator.Calculate();

                await _systemLogger.LogSuccessAsync(
                    "APPLY_COUPON",
                    $"Coupon '{request.CouponCode}' applied: {request.Price:N0} -> {finalPrice:N0} VND",
                    "Anonymous"
                );

                return Ok(new
                {
                    success = true,
                    message = "Coupon applied successfully",
                    data = new
                    {
                        originalPrice = request.Price,
                        finalPrice = finalPrice,
                        discount = request.Price - finalPrice,
                        description = calculator.GetDescription()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying coupon");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error applying coupon",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Calculate checkout total with stacked discounts (Decorator Pattern + Strategy Pattern)
        /// Used by DigiBook checkout process
        /// </summary>
        [HttpPost("calculate-checkout")]
        public async Task<IActionResult> CalculateCheckout([FromBody] CheckoutCalculationRequest request)
        {
            try
            {
                // Start with subtotal
                IPriceCalculator calculator = new BasePriceCalculator(request.Subtotal, "Order");

                var appliedDiscounts = new List<object>();
                var discountBreakdown = new List<string>();

                // 1. Apply Membership Discount (if user is logged in)
                if (!string.IsNullOrEmpty(request.UserId))
                {
                    var user = await _userRepository.GetByIdAsync(request.UserId);
                    if (user != null && !string.IsNullOrEmpty(user.MembershipTier) && user.MembershipTier.ToLower() != "regular")
                    {
                        var beforeMembership = calculator.Calculate();
                        calculator = new MembershipDiscountDecorator(calculator, user.MembershipTier);
                        var afterMembership = calculator.Calculate();
                        var membershipSavings = beforeMembership - afterMembership;

                        appliedDiscounts.Add(new
                        {
                            type = "Membership",
                            tier = user.MembershipTier,
                            savings = membershipSavings
                        });
                        discountBreakdown.Add($"Membership ({user.MembershipTier}): -{membershipSavings:N0}đ");
                    }
                }

                // 2. Apply Coupon Discount (if provided)
                if (!string.IsNullOrEmpty(request.CouponCode))
                {
                    var coupon = await _couponRepository.GetByCodeAsync(request.CouponCode);
                    if (coupon != null && coupon.IsActive)
                    {
                        var beforeCoupon = calculator.Calculate();
                        calculator = new CouponDiscountDecorator(
                            calculator,
                            coupon.Code,
                            coupon.DiscountValue,
                            coupon.DiscountType == "percentage"
                        );
                        var afterCoupon = calculator.Calculate();
                        var couponSavings = beforeCoupon - afterCoupon;

                        appliedDiscounts.Add(new
                        {
                            type = "Coupon",
                            code = coupon.Code,
                            discountType = coupon.DiscountType,
                            discountValue = coupon.DiscountValue,
                            savings = couponSavings
                        });
                        discountBreakdown.Add($"Coupon ({coupon.Code}): -{couponSavings:N0}đ");
                    }
                    else
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Invalid or inactive coupon code"
                        });
                    }
                }

                // 3. Apply Seasonal/Promotional Discount (if active)
                if (request.ApplySeasonalDiscount)
                {
                    var beforeSeasonal = calculator.Calculate();
                    calculator = new SeasonalDiscountDecorator(
                        calculator,
                        "Spring Sale",
                        5, // 5% seasonal discount
                        DateTime.Now.AddDays(-7),
                        DateTime.Now.AddDays(7)
                    );
                    var afterSeasonal = calculator.Calculate();
                    var seasonalSavings = beforeSeasonal - afterSeasonal;

                    if (seasonalSavings > 0)
                    {
                        appliedDiscounts.Add(new
                        {
                            type = "Seasonal",
                            name = "Spring Sale",
                            savings = seasonalSavings
                        });
                        discountBreakdown.Add($"Spring Sale: -{seasonalSavings:N0}đ");
                    }
                }

                // Calculate final total
                var finalSubtotal = calculator.Calculate();
                var totalSavings = request.Subtotal - finalSubtotal;
                var finalTotal = finalSubtotal + request.Shipping;

                await _systemLogger.LogSuccessAsync(
                    "CHECKOUT_CALCULATION",
                    $"Checkout calculated: {request.Subtotal:N0} -> {finalTotal:N0} VND ({appliedDiscounts.Count} discounts)",
                    request.UserId ?? "Anonymous"
                );

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        subtotal = request.Subtotal,
                        shipping = request.Shipping,
                        discountedSubtotal = finalSubtotal,
                        totalSavings = totalSavings,
                        finalTotal = finalTotal,
                        appliedDiscounts = appliedDiscounts,
                        discountBreakdown = discountBreakdown,
                        description = calculator.GetDescription()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating checkout");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error calculating checkout",
                    error = ex.Message
                });
            }
        }

        private IPriceCalculator ApplyDiscount(IPriceCalculator calculator, DiscountItem discount, int quantity)
        {
            return discount.Type.ToUpper() switch
            {
                "PERCENTAGE" => new PercentageDiscountDecorator(
                    calculator, 
                    discount.Value ?? 0, 
                    discount.Reason ?? "Percentage Discount"),

                "FIXED" => new FixedAmountDiscountDecorator(
                    calculator, 
                    discount.Value ?? 0, 
                    discount.Reason ?? "Fixed Discount"),

                "MEMBERSHIP" => new MembershipDiscountDecorator(
                    calculator, 
                    discount.MembershipTier ?? "BRONZE"),

                "COUPON" => new CouponDiscountDecorator(
                    calculator, 
                    discount.CouponCode ?? "UNKNOWN", 
                    discount.Value ?? 0, 
                    discount.IsPercentage ?? true),

                "SEASONAL" => new SeasonalDiscountDecorator(
                    calculator, 
                    discount.SeasonName ?? "Sale", 
                    discount.Value ?? 0, 
                    discount.StartDate ?? DateTime.Now, 
                    discount.EndDate ?? DateTime.Now.AddDays(7)),

                "BULK" => new BulkPurchaseDiscountDecorator(
                    calculator, 
                    quantity),

                _ => calculator
            };
        }
    }

    public class CheckoutCalculationRequest
    {
        public string? UserId { get; set; }
        public double Subtotal { get; set; }
        public double Shipping { get; set; }
        public string? CouponCode { get; set; }
        public bool ApplySeasonalDiscount { get; set; } = false;
    }

    public class SimpleDiscountRequest
    {
        public double Price { get; set; }
        public string ItemName { get; set; } = "Item";
        public int Quantity { get; set; } = 1;
        public string? MembershipTier { get; set; }
    }

    public class CouponRequest
    {
        public double Price { get; set; }
        public string ItemName { get; set; } = "Item";
        public string CouponCode { get; set; } = string.Empty;
    }
}
