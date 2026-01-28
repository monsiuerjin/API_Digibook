using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using API_DigiBook.Models;
using API_DigiBook.Interfaces.Repositories;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CouponsController : ControllerBase
    {
        private readonly ICouponRepository _couponRepository;
        private readonly ILogger<CouponsController> _logger;

        public CouponsController(ICouponRepository couponRepository, ILogger<CouponsController> logger)
        {
            _couponRepository = couponRepository;
            _logger = logger;
        }

        /// <summary>
        /// Get all coupons
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllCoupons()
        {
            try
            {
                var coupons = await _couponRepository.GetAllAsync();

                return Ok(new
                {
                    success = true,
                    count = coupons.Count(),
                    data = coupons
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting coupons");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving coupons",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get coupon by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCouponById(string id)
        {
            try
            {
                var coupon = await _couponRepository.GetByIdAsync(id);

                if (coupon == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Coupon with ID '{id}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = coupon
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting coupon by ID: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving coupon",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get coupon by code
        /// </summary>
        [HttpGet("code/{code}")]
        public async Task<IActionResult> GetCouponByCode(string code)
        {
            try
            {
                var coupon = await _couponRepository.GetByCodeAsync(code);

                if (coupon == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Coupon with code '{code}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = coupon
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting coupon by code: {Code}", code);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving coupon",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get active coupons
        /// </summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveCoupons()
        {
            try
            {
                var coupons = await _couponRepository.GetActiveAsync();

                return Ok(new
                {
                    success = true,
                    count = coupons.Count(),
                    data = coupons
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active coupons");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving coupons",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Validate and apply coupon
        /// </summary>
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateCoupon([FromBody] ValidateCouponRequest request)
        {
            try
            {
                var coupon = await _couponRepository.GetByCodeAsync(request.Code);

                if (coupon == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Coupon not found"
                    });
                }

                if (!coupon.IsActive)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Coupon is not active"
                    });
                }

                if (coupon.UsedCount >= coupon.UsageLimit)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Coupon usage limit exceeded"
                    });
                }

                if (!string.IsNullOrEmpty(coupon.ExpiryDate))
                {
                    if (DateTime.TryParse(coupon.ExpiryDate, out var expiryDate))
                    {
                        if (expiryDate < DateTime.Now)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = "Coupon has expired"
                            });
                        }
                    }
                }

                if (request.OrderTotal < coupon.MinOrderValue)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Minimum order value is {coupon.MinOrderValue:N0} VND"
                    });
                }

                double discount = 0;
                if (coupon.DiscountType == "percentage")
                {
                    discount = request.OrderTotal * coupon.DiscountValue / 100;
                }
                else // fixed
                {
                    discount = coupon.DiscountValue;
                }

                return Ok(new
                {
                    success = true,
                    message = "Coupon is valid",
                    data = new
                    {
                        coupon = coupon,
                        discount = discount,
                        finalTotal = request.OrderTotal - discount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating coupon: {Code}", request.Code);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error validating coupon",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Create a new coupon
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCoupon([FromBody] Coupon coupon)
        {
            try
            {
                // Use code as document ID (uppercase)
                coupon.Id = coupon.Code.ToUpper();
                coupon.Code = coupon.Code.ToUpper();
                coupon.UpdatedAt = Timestamp.GetCurrentTimestamp();

                // Check if coupon already exists
                var exists = await _couponRepository.ExistsAsync(coupon.Id);
                if (exists)
                {
                    return Conflict(new
                    {
                        success = false,
                        message = $"Coupon with code '{coupon.Code}' already exists"
                    });
                }

                await _couponRepository.AddAsync(coupon, coupon.Id);

                return CreatedAtAction(nameof(GetCouponById), new { id = coupon.Id }, new
                {
                    success = true,
                    message = "Coupon created successfully",
                    data = coupon
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating coupon");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error creating coupon",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Update an existing coupon
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCoupon(string id, [FromBody] Coupon coupon)
        {
            try
            {
                coupon.Id = id;
                coupon.UpdatedAt = Timestamp.GetCurrentTimestamp();

                var updated = await _couponRepository.UpdateAsync(id, coupon);

                if (!updated)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Coupon with ID '{id}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Coupon updated successfully",
                    data = coupon
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating coupon with ID: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error updating coupon",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Delete a coupon
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCoupon(string id)
        {
            try
            {
                var deleted = await _couponRepository.DeleteAsync(id);

                if (!deleted)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Coupon with ID '{id}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Coupon deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting coupon with ID: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error deleting coupon",
                    error = ex.Message
                });
            }
        }
    }

    public class ValidateCouponRequest
    {
        public string Code { get; set; } = string.Empty;
        public double OrderTotal { get; set; }
    }
}
