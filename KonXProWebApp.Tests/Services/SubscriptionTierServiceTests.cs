using KonXProWebApp.Data;
using KonXProWebApp.Models.PermitIntel;
using KonXProWebApp.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KonXProWebApp.Tests.Services;

public class SubscriptionTierServiceTests : IDisposable
{
 private readonly db_9f8bee_konxdevContext _context;
 private readonly SubscriptionTierService _service;

 public SubscriptionTierServiceTests()
 {
 var options = new DbContextOptionsBuilder<db_9f8bee_konxdevContext>()
 .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
 .Options;

 _context = new db_9f8bee_konxdevContext(options);
 _service = new SubscriptionTierService(_context);
 }

 public void Dispose() => _context.Dispose();

 [Theory]
 [InlineData("Business", true)]
 [InlineData("Agency", true)]
 [InlineData("LandlordCompliance", true)]
 public void CanAccessOwnerLookup_HighestPricedTiers_ReturnsTrue(string tier, bool expected)
 {
 Assert.Equal(expected, SubscriptionTierService.CanAccessOwnerLookup(tier));
 }

 [Theory]
 [InlineData("Free", false)]
 [InlineData("Starter", false)]
 [InlineData("ComplianceAlerts", false)]
 [InlineData("Pro", false)]
 [InlineData("", false)]
 [InlineData(null, false)]
 public void CanAccessOwnerLookup_LowerTiers_ReturnsFalse(string tier, bool expected)
 {
 Assert.Equal(expected, SubscriptionTierService.CanAccessOwnerLookup(tier));
 }

 [Fact]
 public async Task HasFeatureAccess_OwnerLookup_UserOnBusinessTier_ReturnsTrue()
 {
 var sub = new Subscription
 {
 UserId = "user-biz",
 Tier = "Business",
 Status = "Active",
 CreatedAt = DateTime.UtcNow
 };
 _context.Subscriptions.Add(sub);
 await _context.SaveChangesAsync();

 var hasAccess = await _service.HasFeatureAccess("user-biz", "OwnerLookup");
 Assert.True(hasAccess);
 }

 [Fact]
 public async Task HasFeatureAccess_OwnerLookup_UserOnProTier_ReturnsFalse()
 {
 var sub = new Subscription
 {
 UserId = "user-pro",
 Tier = "Pro",
 Status = "Active",
 CreatedAt = DateTime.UtcNow
 };
 _context.Subscriptions.Add(sub);
 await _context.SaveChangesAsync();

 var hasAccess = await _service.HasFeatureAccess("user-pro", "OwnerLookup");
 Assert.False(hasAccess);
 }

 [Fact]
 public async Task HasFeatureAccess_OwnerLookup_FreeUser_ReturnsFalse()
 {
 var hasAccess = await _service.HasFeatureAccess("user-none", "OwnerLookup");
 Assert.False(hasAccess);
 }
}
