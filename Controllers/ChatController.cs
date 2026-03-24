using API_DigiBook.Interfaces.Services;
using API_DigiBook.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API_DigiBook.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatRecommendationService _chatRecommendationService;
        private readonly ChatbotOptions _options;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            IChatRecommendationService chatRecommendationService,
            IOptions<ChatbotOptions> options,
            ILogger<ChatController> logger)
        {
            _chatRecommendationService = chatRecommendationService;
            _options = options.Value;
            _logger = logger;
        }

        [HttpPost("recommend")]
        public async Task<IActionResult> Recommend(
            [FromBody] ChatRecommendationRequest request,
            CancellationToken cancellationToken)
        {
            if (!_options.Enabled)
            {
                return StatusCode(503, new
                {
                    success = false,
                    message = "Chatbot is disabled"
                });
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Query is required"
                });
            }

            try
            {
                var result = await _chatRecommendationService.GetRecommendationAsync(request, cancellationToken);

                if (string.Equals(result.StrategyUsed, "service-throttled", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(429, new
                    {
                        success = false,
                        message = result.Answer,
                        data = result
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating chat recommendation");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error generating recommendation",
                    error = ex.Message
                });
            }
        }
    }
}
