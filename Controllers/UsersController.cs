using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using API_DigiBook.Models;
using API_DigiBook.Interfaces.Repositories;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserRepository userRepository, ILogger<UsersController> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        /// <summary>
        /// Get all users
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userRepository.GetAllAsync();

                return Ok(new
                {
                    success = true,
                    count = users.Count(),
                    data = users
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving users",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);

                if (user == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"User with ID '{id}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = user
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving user",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get user by email
        /// </summary>
        [HttpGet("email/{email}")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(email);

                if (user == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"User with email '{email}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = user
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving user",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get users by role
        /// </summary>
        [HttpGet("role/{role}")]
        public async Task<IActionResult> GetUsersByRole(string role)
        {
            try
            {
                var users = await _userRepository.GetByRoleAsync(role);

                return Ok(new
                {
                    success = true,
                    count = users.Count(),
                    data = users
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by role: {Role}", role);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving users",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get users by status
        /// </summary>
        [HttpGet("status/{status}")]
        public async Task<IActionResult> GetUsersByStatus(string status)
        {
            try
            {
                var users = await _userRepository.GetByStatusAsync(status);

                return Ok(new
                {
                    success = true,
                    count = users.Count(),
                    data = users
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by status: {Status}", status);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving users",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            try
            {
                // Check if email already exists
                var existingUser = await _userRepository.GetByEmailAsync(user.Email);
                if (existingUser != null)
                {
                    return Conflict(new
                    {
                        success = false,
                        message = $"User with email '{user.Email}' already exists"
                    });
                }

                user.CreatedAt = Timestamp.GetCurrentTimestamp();
                user.UpdatedAt = Timestamp.GetCurrentTimestamp();

                var userId = await _userRepository.AddAsync(user, user.Id);
                user.Id = userId;

                return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new
                {
                    success = true,
                    message = "User created successfully",
                    data = user
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error creating user",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Update an existing user
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] User user)
        {
            try
            {
                user.Id = id;
                user.UpdatedAt = Timestamp.GetCurrentTimestamp();

                var updated = await _userRepository.UpdateAsync(id, user);

                if (!updated)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"User with ID '{id}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "User updated successfully",
                    data = user
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error updating user",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Delete a user
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var deleted = await _userRepository.DeleteAsync(id);

                if (!deleted)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"User with ID '{id}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "User deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error deleting user",
                    error = ex.Message
                });
            }
        }

        // ============ Address Management Endpoints ============

        /// <summary>
        /// Add a new address for a user
        /// </summary>
        [HttpPost("{userId}/addresses")]
        public async Task<IActionResult> AddAddress(string userId, [FromBody] Address address)
        {
            try
            {
                var success = await _userRepository.AddAddressAsync(userId, address);

                if (!success)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"User with ID '{userId}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Address added successfully",
                    data = address
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding address for user {UserId}", userId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error adding address",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Update an address for a user
        /// </summary>
        [HttpPut("{userId}/addresses/{addressId}")]
        public async Task<IActionResult> UpdateAddress(string userId, string addressId, [FromBody] Address address)
        {
            try
            {
                var success = await _userRepository.UpdateAddressAsync(userId, addressId, address);

                if (!success)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Address '{addressId}' not found for user '{userId}'"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Address updated successfully",
                    data = address
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating address {AddressId} for user {UserId}", addressId, userId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error updating address",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Delete an address for a user
        /// </summary>
        [HttpDelete("{userId}/addresses/{addressId}")]
        public async Task<IActionResult> DeleteAddress(string userId, string addressId)
        {
            try
            {
                var success = await _userRepository.DeleteAddressAsync(userId, addressId);

                if (!success)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Address '{addressId}' not found for user '{userId}'"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Address deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting address {AddressId} for user {UserId}", addressId, userId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error deleting address",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Set an address as default for a user
        /// </summary>
        [HttpPatch("{userId}/addresses/{addressId}/set-default")]
        public async Task<IActionResult> SetDefaultAddress(string userId, string addressId)
        {
            try
            {
                var success = await _userRepository.SetDefaultAddressAsync(userId, addressId);

                if (!success)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Address '{addressId}' not found for user '{userId}'"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Default address set successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default address {AddressId} for user {UserId}", addressId, userId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error setting default address",
                    error = ex.Message
                });
            }
        }

        // ============ Wishlist Management Endpoints ============

        /// <summary>
        /// Add a book to user's wishlist
        /// </summary>
        [HttpPost("{userId}/wishlist/{bookId}")]
        public async Task<IActionResult> AddToWishlist(string userId, string bookId)
        {
            try
            {
                var success = await _userRepository.AddToWishlistAsync(userId, bookId);

                if (!success)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"User with ID '{userId}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Book added to wishlist successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding book {BookId} to wishlist for user {UserId}", bookId, userId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error adding to wishlist",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Remove a book from user's wishlist
        /// </summary>
        [HttpDelete("{userId}/wishlist/{bookId}")]
        public async Task<IActionResult> RemoveFromWishlist(string userId, string bookId)
        {
            try
            {
                var success = await _userRepository.RemoveFromWishlistAsync(userId, bookId);

                if (!success)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"User with ID '{userId}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Book removed from wishlist successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing book {BookId} from wishlist for user {UserId}", bookId, userId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error removing from wishlist",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get user's wishlist
        /// </summary>
        [HttpGet("{userId}/wishlist")]
        public async Task<IActionResult> GetWishlist(string userId)
        {
            try
            {
                var wishlistIds = await _userRepository.GetWishlistAsync(userId);

                return Ok(new
                {
                    success = true,
                    count = wishlistIds.Count,
                    data = wishlistIds
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wishlist for user {UserId}", userId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving wishlist",
                    error = ex.Message
                });
            }
        }
    }
}
