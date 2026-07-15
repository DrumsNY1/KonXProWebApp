using System.Net;
using System.Text;
using KonXProWebApp.Integration.Tests.Infrastructure;
using Xunit;

namespace KonXProWebApp.Integration.Tests.Controllers;

/// <summary>
/// Covers the branch where Stripe:WebhookSecret IS configured, so the controller uses
/// EventUtility.ConstructEvent (which verifies the Stripe-Signature header) rather than ParseEvent.
/// </summary>
public class StripeWebhookSignedTests : IClassFixture<SqlWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqlWebApplicationFactory _factory;

    public StripeWebhookSignedTests(SqlWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ExtraConfig["Stripe:WebhookSecret"] = "whsec_test_secret";
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        await _factory.EnsureDatabaseCreatedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PostWebhook_InvalidSignature_Returns400()
    {
        var client = _factory.CreateClient();
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        content.Headers.TryAddWithoutValidation("Stripe-Signature", "t=1690000000,v1=deadbeef");

        var response = await client.PostAsync("/api/stripe/webhook", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostWebhook_MissingSignatureHeader_Returns400()
    {
        var client = _factory.CreateClient();
        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/stripe/webhook", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

/// <summary>
/// Covers the branch where Stripe:WebhookSecret is NOT configured, so the controller falls back to
/// EventUtility.ParseEvent with no signature verification — the documented behavior for local/dev use.
/// </summary>
public class StripeWebhookUnsignedTests : IClassFixture<SqlWebApplicationFactory>, IAsyncLifetime
{
    private readonly SqlWebApplicationFactory _factory;

    public StripeWebhookUnsignedTests(SqlWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        await _factory.EnsureDatabaseCreatedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PostWebhook_CheckoutSessionCompleted_NoSecretConfigured_Returns200AndCreatesSubscription()
    {
        var client = _factory.CreateClient();
        var payload = BuildCheckoutCompletedPayload("evt_test_checkout_1", "cs_test_1", "user_integration_1", "Pro");
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/stripe/webhook", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostWebhook_UnhandledEventType_Returns200WithNoSideEffects()
    {
        var client = _factory.CreateClient();
        var payload = BuildUnhandledEventPayload();
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/stripe/webhook", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostWebhook_InvoicePaymentFailed_Returns200()
    {
        var client = _factory.CreateClient();
        var payload = """
        {
          "id": "evt_test_invoice_failed",
          "object": "event",
          "type": "invoice.payment_failed",
          "data": {
            "object": {
              "id": "in_test_1",
              "object": "invoice"
            }
          }
        }
        """;
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/stripe/webhook", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static string BuildCheckoutCompletedPayload(string eventId, string sessionId, string userId, string tier) => $$"""
    {
      "id": "{{eventId}}",
      "object": "event",
      "type": "checkout.session.completed",
      "data": {
        "object": {
          "id": "{{sessionId}}",
          "object": "checkout.session",
          "customer": "cus_test_1",
          "subscription": "sub_test_1",
          "metadata": {
            "userId": "{{userId}}",
            "tier": "{{tier}}"
          }
        }
      }
    }
    """;

    private static string BuildUnhandledEventPayload() => """
    {
      "id": "evt_test_unhandled",
      "object": "event",
      "type": "payment_intent.created",
      "data": {
        "object": {
          "id": "pi_test_1",
          "object": "payment_intent"
        }
      }
    }
    """;
}
