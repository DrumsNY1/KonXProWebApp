using KonXProWebApp.Functions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KonXProWebApp.Functions.Tests.Services;

public class SmsServiceTests
{
    [Fact]
    public void BuildAlertSms_FormatsCorrectly()
    {
        var msg = SmsService.BuildAlertSms(5, "456 Atlantic Ave");

        Assert.Contains("5", msg);
        Assert.Contains("456 Atlantic Ave", msg);
        Assert.Contains("konxpro.com", msg);
        Assert.True(msg.Length <= 160, "SMS should fit in one message segment");
    }

    [Fact]
    public void BuildAlertSms_SingleMatch_UsesSingularFriendlyText()
    {
        var msg = SmsService.BuildAlertSms(1, "1 Only St");

        Assert.Contains("1 new permit(s)", msg);
    }

    [Fact]
    public async Task SendSmsAsync_NotConfigured_ReturnsFalseWithoutThrowing()
    {
        // No TwilioAccountSid/TwilioAuthToken configured → _isConfigured is false.
        var config = new ConfigurationBuilder().Build();
        var service = new SmsService(config, new Mock<ILogger<SmsService>>().Object);

        var result = await service.SendSmsAsync("+15555550100", "test message");

        Assert.False(result);
    }
}
