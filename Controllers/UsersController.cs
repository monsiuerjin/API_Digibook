using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using API_DigiBook.Models;
using API_DigiBook.Repositories;

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
    }
}
