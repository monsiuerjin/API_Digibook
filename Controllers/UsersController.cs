using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using API_DigiBook.Models;
using API_DigiBook.Interfaces.Repositories;
using API_DigiBook.Interfaces.Services;
using System.Text.Json;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UsersController> _logger;
        private readonly ICacheService _cache;

        public UsersController(IUserRepository userRepository, ILogger<UsersController> logger, ICacheService cache)
        {
            _userRepository = userRepository;
            _logger = logger;
            _cache = cache;
        }

        /// <summary>
        /// Get all users
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var cacheKey = _cache.GetVersionedKey("users:all");
                var users = await _cache.GetOrSetAsync(cacheKey, async () => 
                {
                    var all = await _userRepository.GetAllAsync();
                    return all.ToList();
                });

                return Ok(new
                {
                    success = true,
                    count = users?.Count ?? 0,
                    data = users
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return StatusCode(500, new { success = false, message = "Error retrieving users", error = ex.Message });
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
                var cacheKey = _cache.GetVersionedKey($"users:id:{id}");
                var user = await _cache.GetOrSetAsync(cacheKey, () => _userRepository.GetByIdAsync(id));

                if (user == null)
                {
                    return NotFound(new { success = false, message = $"User with ID '{id}' not found" });
                }

                return Ok(new { success = true, data = user });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Error retrieving user", error = ex.Message });
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
                var cacheKey = _cache.GetVersionedKey($"users:email:{email}");
                var user = await _cache.GetOrSetAsync(cacheKey, () => _userRepository.GetByEmailAsync(email));

                if (user == null)
                {
                    return NotFound(new { success = false, message = $"User with email '{email}' not found" });
                }

                return Ok(new { success = true, data = user });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                return StatusCode(500, new { success = false, message = "Error retrieving user", error = ex.Message });
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
                return Ok(new { success = true, count = users.Count(), data = users });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by role: {Role}", role);
                return StatusCode(500, new { success = false, message = "Error retrieving users", error = ex.Message });
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
                return Ok(new { success = true, count = users.Count(), data = users });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by status: {Status}", status);
                return StatusCode(500, new { success = false, message = "Error retrieving users", error = ex.Message });
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
                var existingUser = await _userRepository.GetByEmailAsync(user.Email);
                if (existingUser != null)
                {
                    return Conflict(new { success = false, message = $"User with email '{user.Email}' already exists" });
                }

                user.CreatedAt = Timestamp.GetCurrentTimestamp();
                user.UpdatedAt = Timestamp.GetCurrentTimestamp();

                var userId = await _userRepository.AddAsync(user, user.Id);
                user.Id = userId;
                _cache.BumpVersion("users");

                return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new { success = true, message = "User created successfully", data = user });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new { success = false, message = "Error creating user", error = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing user
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] JsonElement updates)
        {
            try
            {
                var updateDict = JsonElementToDictionary(updates);
                if (updateDict.Count == 0)
                {
                    return BadRequest(new { success = false, message = "At least one field must be provided for update" });
                }

                updateDict["updatedAt"] = Timestamp.GetCurrentTimestamp();
                var updated = await _userRepository.UpdateFieldsAsync(id, updateDict);

                if (!updated)
                {
                    return NotFound(new { success = false, message = $"User with ID '{id}' not found" });
                }

                _cache.BumpVersion("users");
                return Ok(new { success = true, message = "User updated successfully", data = await _userRepository.GetByIdAsync(id) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Error updating user", error = ex.Message });
            }
        }

        /// <summary>
        /// Update user role (admin)
        /// </summary>
        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateUserRole(string id, [FromBody] JsonElement body)
        {
            try
            {
                var role = ExtractString(body, "role");
                if (string.IsNullOrWhiteSpace(role))
                {
                    return BadRequest(new { success = false, message = "Role is required" });
                }

                var updated = await _userRepository.UpdateFieldsAsync(id, new Dictionary<string, object>
                {
                    ["role"] = role,
                    ["updatedAt"] = Timestamp.GetCurrentTimestamp()
                });

                if (!updated)
                {
                    return NotFound(new { success = false, message = $"User with ID '{id}' not found" });
                }

                _cache.BumpVersion("users");
                return Ok(new { success = true, message = "User role updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user role with ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Error updating user role", error = ex.Message });
            }
        }

        /// <summary>
        /// Update user status (admin)
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(string id, [FromBody] JsonElement body)
        {
            try
            {
                var status = ExtractString(body, "status");
                if (string.IsNullOrWhiteSpace(status))
                {
                    return BadRequest(new { success = false, message = "Status is required" });
                }

                var updated = await _userRepository.UpdateFieldsAsync(id, new Dictionary<string, object>
                {
                    ["status"] = status,
                    ["updatedAt"] = Timestamp.GetCurrentTimestamp()
                });

                if (!updated)
                {
                    return NotFound(new { success = false, message = $"User with ID '{id}' not found" });
                }

                _cache.BumpVersion("users");
                return Ok(new { success = true, message = "User status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user status with ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Error updating user status", error = ex.Message });
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
                    return NotFound(new { success = false, message = $"User with ID '{id}' not found" });
                }

                _cache.BumpVersion("users");
                return Ok(new { success = true, message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID: {Id}", id);
                return StatusCode(500, new { success = false, message = "Error deleting user", error = ex.Message });
            }
        }

        // ============ Address Management Endpoints ============
        [HttpPost("{userId}/addresses")]
        public async Task<IActionResult> AddAddress(string userId, [FromBody] Address address)
        {
            try
            {
                var success = await _userRepository.AddAddressAsync(userId, address);
                if (!success)
                {
                    return NotFound(new { success = false, message = $"User with ID '{userId}' not found" });
                }
                _cache.BumpVersion("users");
                return Ok(new { success = true, message = "Address added successfully", data = address });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding address for user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Error adding address", error = ex.Message });
            }
        }

        [HttpPut("{userId}/addresses/{addressId}")]
        public async Task<IActionResult> UpdateAddress(string userId, string addressId, [FromBody] Address address)
        {
            try
            {
                var success = await _userRepository.UpdateAddressAsync(userId, addressId, address);
                if (!success)
                {
                    return NotFound(new { success = false, message = $"Address '{addressId}' not found for user '{userId}'" });
                }
                _cache.BumpVersion("users");
                return Ok(new { success = true, message = "Address updated successfully", data = address });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating address {AddressId} for user {UserId}", addressId, userId);
                return StatusCode(500, new { success = false, message = "Error updating address", error = ex.Message });
            }
        }

        [HttpDelete("{userId}/addresses/{addressId}")]
        public async Task<IActionResult> DeleteAddress(string userId, string addressId)
        {
            try
            {
                var success = await _userRepository.DeleteAddressAsync(userId, addressId);
                if (!success)
                {
                    return NotFound(new { success = false, message = $"Address '{addressId}' not found for user '{userId}'" });
                }
                _cache.BumpVersion("users");
                return Ok(new { success = true, message = "Address deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting address {AddressId} for user {UserId}", addressId, userId);
                return StatusCode(500, new { success = false, message = "Error deleting address", error = ex.Message });
            }
        }

        [HttpPatch("{userId}/addresses/{addressId}/set-default")]
        public async Task<IActionResult> SetDefaultAddress(string userId, string addressId)
        {
            try
            {
                var success = await _userRepository.SetDefaultAddressAsync(userId, addressId);
                if (!success)
                {
                    return NotFound(new { success = false, message = $"Address '{addressId}' not found for user '{userId}'" });
                }
                _cache.BumpVersion("users");
                return Ok(new { success = true, message = "Default address set successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default address {AddressId} for user {UserId}", addressId, userId);
                return StatusCode(500, new { success = false, message = "Error setting default address", error = ex.Message });
            }
        }

        [HttpPut("{userId}/addresses/{addressId}/set-default")]
        public async Task<IActionResult> SetDefaultAddressPut(string userId, string addressId)
        {
            return await SetDefaultAddress(userId, addressId);
        }

        // ============ Wishlist Management Endpoints ============
        [HttpPost("{userId}/wishlist/{bookId}")]
        public async Task<IActionResult> AddToWishlist(string userId, string bookId)
        {
            try
            {
                var success = await _userRepository.AddToWishlistAsync(userId, bookId);
                if (!success)
                {
                    return NotFound(new { success = false, message = $"User with ID '{userId}' not found" });
                }
                _cache.BumpVersion("users");
                return Ok(new { success = true, message = "Book added to wishlist successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding book {BookId} to wishlist for user {UserId}", bookId, userId);
                return StatusCode(500, new { success = false, message = "Error adding to wishlist", error = ex.Message });
            }
        }

        [HttpDelete("{userId}/wishlist/{bookId}")]
        public async Task<IActionResult> RemoveFromWishlist(string userId, string bookId)
        {
            try
            {
                var success = await _userRepository.RemoveFromWishlistAsync(userId, bookId);
                if (!success)
                {
                    return NotFound(new { success = false, message = $"User with ID '{userId}' not found" });
                }
                _cache.BumpVersion("users");
                return Ok(new { success = true, message = "Book removed from wishlist successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing book {BookId} from wishlist for user {UserId}", bookId, userId);
                return StatusCode(500, new { success = false, message = "Error removing from wishlist", error = ex.Message });
            }
        }

        [HttpGet("{userId}/wishlist")]
        public async Task<IActionResult> GetWishlist(string userId)
        {
            try
            {
                var wishlistIds = await _userRepository.GetWishlistAsync(userId);
                return Ok(new { success = true, count = wishlistIds.Count, data = wishlistIds });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wishlist for user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Error retrieving wishlist", error = ex.Message });
            }
        }

        [HttpPut("{userId}/wishlist")]
        public async Task<IActionResult> UpdateWishlist(string userId, [FromBody] JsonElement body)
        {
            try
            {
                var wishlistIds = ExtractStringList(body, "wishlistIds");
                if (wishlistIds == null)
                {
                    return BadRequest(new { success = false, message = "wishlistIds is required" });
                }

                var updated = await _userRepository.UpdateFieldsAsync(userId, new Dictionary<string, object>
                {
                    ["wishlistIds"] = wishlistIds,
                    ["updatedAt"] = Timestamp.GetCurrentTimestamp()
                });

                if (!updated)
                {
                    return NotFound(new { success = false, message = $"User with ID '{userId}' not found" });
                }

                _cache.BumpVersion("users");
                return Ok(new { success = true, message = "Wishlist updated successfully", data = wishlistIds });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating wishlist for user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Error updating wishlist", error = ex.Message });
            }
        }

        private static Dictionary<string, object> JsonElementToDictionary(JsonElement element)
        {
            var result = new Dictionary<string, object>();
            if (element.ValueKind != JsonValueKind.Object) return result;

            foreach (var property in element.EnumerateObject())
            {
                result[property.Name] = ConvertJsonElement(property.Value);
            }
            return result;
        }

        private static object? ConvertJsonElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
                JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
                _ => null
            };
        }

        private static string? ExtractString(JsonElement body, string propertyName)
        {
            if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty(propertyName, out var value))
            {
                if (value.ValueKind == JsonValueKind.String) return value.GetString();
            }
            return null;
        }

        private static List<string>? ExtractStringList(JsonElement body, string propertyName)
        {
            if (body.ValueKind == JsonValueKind.Array)
            {
                return body.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            }
            if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty(propertyName, out var value))
            {
                if (value.ValueKind == JsonValueKind.Array)
                {
                    return value.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                }
            }
            return null;
        }
    }
}
