using Microsoft.AspNetCore.Mvc;
using Stripe;
using KonXProWebApp.Services;

namespace KonXProWebApp.Controllers;

/// <summary>
/// Handles Stripe webhook events for subscription lifecycle management.
/// Endpoint: POST /api/stripe/webhook
/// </summary>
[ApiController]
[Route("api/stripe")]
public class StripeWebhookController : ControllerBase
{
    private readonly StripeService _stripeService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        StripeService stripeService,
        IConfiguration configuration,
        ILogger<StripeWebhookController> logger)
    {
        _stripeService = stripeService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var webhookSecret = _configuration["Stripe:WebhookSecret"] ?? "";

        try
        {
            var stripeEvent = string.IsNullOrEmpty(webhookSecret)
                ? EventUtility.ParseEvent(json)
                : EventUtility.ConstructEvent(json,
                    Request.Headers["Stripe-Signature"], webhookSecret);

            _logger.LogInformation("Stripe webhook received: {EventType} ({EventId})",
                stripeEvent.Type, stripeEvent.Id);

            switch (stripeEvent.Type)
            {
                case EventTypes.CheckoutSessionCompleted:
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    if (session != null)
                        await _stripeService.HandleCheckoutCompleted(session);
                    break;

                case EventTypes.CustomerSubscriptionUpdated:
                case EventTypes.CustomerSubscriptionDeleted:
                    var subscription = stripeEvent.Data.Object as Stripe.Subscription;
                    if (subscription != null)
                        await _stripeService.HandleSubscriptionUpdated(subscription);
                    break;

                case EventTypes.InvoicePaymentFailed:
                    _logger.LogWarning("Payment failed for invoice: {InvoiceId}",
                        (stripeEvent.Data.Object as Stripe.Invoice)?.Id);
                    break;

                default:
                    _logger.LogDebug("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                    break;
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook signature verification failed");
            return BadRequest("Webhook signature verification failed");
        }
    }
}
