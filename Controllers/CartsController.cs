using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using API_DigiBook.Services;
using System.Text.Json;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartsController : ControllerBase
    {
        private readonly FirestoreDb _db;
        private readonly ILogger<CartsController> _logger;

        public CartsController(ILogger<CartsController> logger)
        {
            _db = FirebaseService.GetFirestoreDb();
            _logger = logger;
        }

        /// <summary>
        /// Get user cart items
        /// </summary>
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetCart(string userId)
        {
            try
            {
                var docRef = _db.Collection("userCarts").Document(userId);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    return Ok(new
                    {
                        success = true,
                        data = new { items = new List<object>() }
                    });
                }

                var data = snapshot.ToDictionary();
                data.TryGetValue("items", out var itemsObj);

                return Ok(new
                {
                    success = true,
                    data = new { items = itemsObj ?? new List<object>() }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart for user {UserId}", userId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving cart",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Replace cart items for a user
        /// </summary>
        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateCart(string userId, [FromBody] JsonElement body)
        {
            try
            {
                var items = ExtractItems(body);
                var docRef = _db.Collection("userCarts").Document(userId);

                await docRef.SetAsync(new Dictionary<string, object>
                {
                    ["items"] = items,
                    ["updatedAt"] = Timestamp.GetCurrentTimestamp()
                }, SetOptions.MergeAll);

                return Ok(new
                {
                    success = true,
                    message = "Cart updated successfully",
                    data = new { items }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart for user {UserId}", userId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error updating cart",
                    error = ex.Message
                });
            }
        }

        private static List<object> ExtractItems(JsonElement body)
        {
            if (body.ValueKind == JsonValueKind.Array)
            {
                return body.EnumerateArray()
                    .Select(ConvertJsonElement)
                    .Where(x => x != null)
                    .ToList()!;
            }

            if (body.ValueKind == JsonValueKind.Object &&
                body.TryGetProperty("items", out var itemsElement) &&
                itemsElement.ValueKind == JsonValueKind.Array)
            {
                return itemsElement.EnumerateArray()
                    .Select(ConvertJsonElement)
                    .Where(x => x != null)
                    .ToList()!;
            }

            return new List<object>();
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
                JsonValueKind.Object => element.EnumerateObject()
                    .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
                _ => null
            };
        }
    }
}
