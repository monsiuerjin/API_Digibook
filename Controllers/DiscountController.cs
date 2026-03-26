using Microsoft.AspNetCore.Mvc;
using API_DigiBook.Models;
using API_DigiBook.Interfaces.Services;
using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Decorator;
using API_DigiBook.Decorator.Decorators;
using API_DigiBook.Singleton;
using System.Text.Json;

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
        public async Task<IActionResult> BlackFridaySale([FromBody] JsonElement request)
        {
            try
            {
                if (request.ValueKind == JsonValueKind.Object && request.TryGetProperty("items", out var itemsElement))
                {
                    var items = ParseBlackFridayItems(itemsElement);
                    var originalTotal = items.Sum(i => i.Price * i.Quantity);
                    var finalTotal = originalTotal * 0.7; // 30% off
                    var totalSavings = originalTotal - finalTotal;
                    var savingsPercentage = originalTotal > 0 ? (totalSavings / originalTotal) * 100 : 0;

                    await _systemLogger.LogSuccessAsync(
                        "BLACK_FRIDAY_SALE",
                        $"Black Friday: {originalTotal:N0} -> {finalTotal:N0} VND",
                        "Anonymous"
                    );

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            originalTotal = originalTotal,
                            finalTotal = finalTotal,
                            totalSavings = totalSavings,
                            savingsPercentage = Math.Round(savingsPercentage, 2),
                            message = "Black Friday discount applied"
                        }
                    });
                }

                var simple = ParseSimpleDiscountRequest(request);
                IPriceCalculator calculator = new BasePriceCalculator(simple.Price, simple.ItemName);

                // Black Friday: 30% off
                calculator = new PercentageDiscountDecorator(calculator, 30, "Black Friday Sale");

                // Additional member discount if provided
                if (!string.IsNullOrEmpty(simple.MembershipTier))
                {
                    calculator = new MembershipDiscountDecorator(calculator, simple.MembershipTier);
                }

                // Bulk discount if buying multiple
                if (simple.Quantity > 1)
                {
                    calculator = new BulkPurchaseDiscountDecorator(calculator, simple.Quantity);
                }

                var finalPrice = calculator.Calculate();

                await _systemLogger.LogSuccessAsync(
                    "BLACK_FRIDAY_SALE",
                    $"Black Friday: {simple.Price:N0} -> {finalPrice:N0} VND",
                    simple.MembershipTier ?? "Anonymous"
                );

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        originalPrice = simple.Price,
                        finalPrice = finalPrice,
                        discount = simple.Price - finalPrice,
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
        public async Task<IActionResult> ApplyCoupon([FromBody] JsonElement request)
        {
            try
            {
                var couponCode = ExtractString(request, "code") ?? ExtractString(request, "couponCode") ?? string.Empty;
                var price = ExtractDouble(request, "price");
                var itemName = ExtractString(request, "itemName") ?? "Item";

                // Simulate coupon validation (in real app, check database)
                var couponDiscounts = new Dictionary<string, (double value, bool isPercentage)>
                {
                    { "SAVE10", (10, true) },
                    { "SAVE20", (20, true) },
                    { "NEWYEAR", (50000, false) },
                    { "WELCOME", (15, true) }
                };

                if (string.IsNullOrWhiteSpace(couponCode) || !couponDiscounts.ContainsKey(couponCode.ToUpper()))
                {
                    await _systemLogger.LogWarningAsync(
                        "INVALID_COUPON",
                        $"Invalid coupon code: {couponCode}",
                        "Anonymous"
                    );

                    return BadRequest(new
                    {
                        success = false,
                        message = $"Invalid coupon code: {couponCode}"
                    });
                }

                var (value, isPercentage) = couponDiscounts[couponCode.ToUpper()];

                IPriceCalculator calculator = new BasePriceCalculator(price, itemName);
                calculator = new CouponDiscountDecorator(calculator, couponCode, value, isPercentage);

                var finalPrice = calculator.Calculate();

                await _systemLogger.LogSuccessAsync(
                    "APPLY_COUPON",
                    $"Coupon '{couponCode}' applied: {price:N0} -> {finalPrice:N0} VND",
                    "Anonymous"
                );

                return Ok(new
                {
                    success = true,
                    message = "Coupon applied successfully",
                    data = new
                    {
                        valid = true,
                        discount = price - finalPrice,
                        finalPrice = finalPrice,
                        message = "Coupon applied successfully",
                        originalPrice = price,
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
        public async Task<IActionResult> CalculateCheckout([FromBody] JsonElement request)
        {
            try
            {
                var parsed = ParseCheckoutRequest(request);

                var subtotal = parsed.Subtotal;
                var shipping = parsed.Shipping;
                var userId = parsed.UserId;
                var couponCode = parsed.CouponCode;
                var applySeasonalDiscount = parsed.ApplySeasonalDiscount;

                // Start with subtotal
                IPriceCalculator calculator = new BasePriceCalculator(subtotal, "Order");

                var appliedDiscounts = new List<object>();
                var discountBreakdown = new List<string>();
                double membershipSavings = 0;
                double couponSavings = 0;
                double seasonalSavings = 0;

                // 1. Apply Membership Discount (if user is logged in)
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user != null && !string.IsNullOrEmpty(user.MembershipTier) && user.MembershipTier.ToLower() != "regular")
                    {
                        var beforeMembership = calculator.Calculate();
                        calculator = new MembershipDiscountDecorator(calculator, user.MembershipTier);
                        var afterMembership = calculator.Calculate();
                        membershipSavings = beforeMembership - afterMembership;

                        appliedDiscounts.Add(new
                        {
                            type = "Membership",
                            tier = user.MembershipTier,
                            savings = membershipSavings
                        });
                        discountBreakdown.Add($"Membership ({user.MembershipTier}): -{membershipSavings:N0}??");
                    }
                }

                // 2. Apply Coupon Discount (if provided)
                if (!string.IsNullOrEmpty(couponCode))
                {
                    var coupon = await _couponRepository.GetByCodeAsync(couponCode);
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
                        couponSavings = beforeCoupon - afterCoupon;

                        appliedDiscounts.Add(new
                        {
                            type = "Coupon",
                            code = coupon.Code,
                            discountType = coupon.DiscountType,
                            discountValue = coupon.DiscountValue,
                            savings = couponSavings
                        });
                        discountBreakdown.Add($"Coupon ({coupon.Code}): -{couponSavings:N0}??");
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
                if (applySeasonalDiscount)
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
                    seasonalSavings = beforeSeasonal - afterSeasonal;

                    if (seasonalSavings > 0)
                    {
                        appliedDiscounts.Add(new
                        {
                            type = "Seasonal",
                            name = "Spring Sale",
                            savings = seasonalSavings
                        });
                        discountBreakdown.Add($"Spring Sale: -{seasonalSavings:N0}??");
                    }
                }

                // Calculate final total
                var finalSubtotal = calculator.Calculate();
                var totalSavings = subtotal - finalSubtotal;
                var finalTotal = finalSubtotal + shipping;

                await _systemLogger.LogSuccessAsync(
                    "CHECKOUT_CALCULATION",
                    $"Checkout calculated: {subtotal:N0} -> {finalTotal:N0} VND ({appliedDiscounts.Count} discounts)",
                    userId ?? "Anonymous"
                );

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        subtotal = subtotal,
                        membershipDiscount = membershipSavings,
                        couponDiscount = couponSavings,
                        seasonalDiscount = seasonalSavings,
                        total = finalTotal,
                        discountsApplied = discountBreakdown
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

        private static SimpleDiscountRequest ParseSimpleDiscountRequest(JsonElement element)
        {
            var result = new SimpleDiscountRequest();

            if (element.ValueKind != JsonValueKind.Object)
            {
                return result;
            }

            if (element.TryGetProperty("price", out var priceElement) && priceElement.TryGetDouble(out var price))
            {
                result.Price = price;
            }

            if (element.TryGetProperty("itemName", out var itemNameElement) && itemNameElement.ValueKind == JsonValueKind.String)
            {
                result.ItemName = itemNameElement.GetString() ?? result.ItemName;
            }

            if (element.TryGetProperty("quantity", out var quantityElement) && quantityElement.TryGetInt32(out var quantity))
            {
                result.Quantity = quantity;
            }

            if (element.TryGetProperty("membershipTier", out var tierElement) && tierElement.ValueKind == JsonValueKind.String)
            {
                result.MembershipTier = tierElement.GetString();
            }

            return result;
        }

        private static List<BlackFridayItem> ParseBlackFridayItems(JsonElement itemsElement)
        {
            var items = new List<BlackFridayItem>();
            if (itemsElement.ValueKind != JsonValueKind.Array)
            {
                return items;
            }

            foreach (var item in itemsElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var name = ExtractString(item, "name") ?? "Item";
                var price = ExtractDouble(item, "price");
                var quantity = ExtractInt(item, "quantity", 1);

                items.Add(new BlackFridayItem
                {
                    Name = name,
                    Price = price,
                    Quantity = quantity
                });
            }

            return items;
        }

        private static CheckoutCalculationRequest ParseCheckoutRequest(JsonElement element)
        {
            var request = new CheckoutCalculationRequest
            {
                ApplySeasonalDiscount = false
            };

            if (element.ValueKind != JsonValueKind.Object)
            {
                return request;
            }

            request.UserId = ExtractString(element, "userId");
            request.CouponCode = ExtractString(element, "couponCode");

            if (element.TryGetProperty("applySeasonalDiscount", out var seasonalElement) &&
                seasonalElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                request.ApplySeasonalDiscount = seasonalElement.GetBoolean();
            }

            if (element.TryGetProperty("subtotal", out var subtotalElement) && subtotalElement.TryGetDouble(out var subtotal))
            {
                request.Subtotal = subtotal;
            }

            if (element.TryGetProperty("shipping", out var shippingElement) && shippingElement.TryGetDouble(out var shipping))
            {
                request.Shipping = shipping;
            }

            if (element.TryGetProperty("items", out var itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
            {
                var items = itemsElement.EnumerateArray()
                    .Where(x => x.ValueKind == JsonValueKind.Object)
                    .Select(x => new
                    {
                        Quantity = ExtractInt(x, "quantity", 1),
                        BasePrice = ExtractDouble(x, "basePrice")
                    })
                    .ToList();

                var computedSubtotal = items.Sum(i => i.BasePrice * i.Quantity);
                request.Subtotal = computedSubtotal;
                request.Shipping = computedSubtotal > 500000 ? 0 : 25000;
            }

            return request;
        }

        private static string? ExtractString(JsonElement element, string propertyName)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(propertyName, out var value) &&
                value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }

            return null;
        }

        private static double ExtractDouble(JsonElement element, string propertyName)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(propertyName, out var value) &&
                value.ValueKind == JsonValueKind.Number)
            {
                return value.GetDouble();
            }

            return 0;
        }

        private static int ExtractInt(JsonElement element, string propertyName, int fallback = 0)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(propertyName, out var value) &&
                value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt32(out var number))
            {
                return number;
            }

            return fallback;
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

    public class BlackFridayItem
    {
        public string Name { get; set; } = "Item";
        public double Price { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
