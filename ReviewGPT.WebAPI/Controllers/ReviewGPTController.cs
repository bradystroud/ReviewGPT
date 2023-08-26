using Microsoft.AspNetCore.Mvc;
using ReviewGPT.Application;

namespace ReviewGPT.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class ReviewGptController : ControllerBase
{
    private readonly ILogger<ReviewGptController> _logger;
    private readonly GenerateReviewService _generateReviewService;

    public ReviewGptController(ILogger<ReviewGptController> logger)
    {
        _logger = logger;
        _generateReviewService = new GenerateReviewService();
    }

    [HttpGet(Name = "GetPrReview")]
    public async Task<string> Get(string prUrl)
    {
        return await _generateReviewService.GenerateReview(prUrl);
    }
}
