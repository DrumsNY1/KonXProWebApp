using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KonXProWebApp.Services;

namespace KonXProWebApp.Controllers;

[ApiController]
[Route("api/stripe")]
public class StripeCheckoutController : ControllerBase
{
    private readonly StripeService _stripeService;
    private readonly ILogger<StripeCheckoutController> _logger;

    public StripeCheckoutController(
        StripeService stripeService,
        ILogger<StripeCheckoutController> logger)
    {
        _stripeService = stripeService;
        _logger = logger;
    }

    [HttpPost("create-checkout-session")]
    [Authorize]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CheckoutRequest request)
    {
        if (string.IsNullOrEmpty(request.Tier))
            return BadRequest(new { error = "Tier is required." });

        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? request.UserId;
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                ?? request.Email;

            var successUrl = request.SuccessUrl ?? $"{Request.Scheme}://{Request.Host}/subscription";
            var cancelUrl = request.CancelUrl ?? $"{Request.Scheme}://{Request.Host}/subscription";

            var checkoutUrl = await _stripeService.CreateCheckoutSession(
                userId, email, request.Tier, successUrl, cancelUrl);

            return Ok(new { url = checkoutUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Stripe checkout session for tier {Tier}", request.Tier);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("create-portal-session")]
    [Authorize]
    public async Task<IActionResult> CreatePortalSession([FromBody] PortalRequest request)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? request.UserId;

            var returnUrl = request.ReturnUrl ?? $"{Request.Scheme}://{Request.Host}/subscription";
            var portalUrl = await _stripeService.CreatePortalSession(userId, returnUrl);

            return Ok(new { url = portalUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Stripe portal session");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class CheckoutRequest
{
    public string UserId { get; set; }
    public string Email { get; set; }
    public string Tier { get; set; }
    public string SuccessUrl { get; set; }
    public string CancelUrl { get; set; }
}

public class PortalRequest
{
    public string UserId { get; set; }
    public string ReturnUrl { get; set; }
}
