using KonXProWebApp.Data;
using KonXProWebApp.Models.db_9f8bee_konxdev;
using KonXProWebApp.Models.PermitIntel;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace KonXProWebApp.Services;

public partial class PermitIntelService
{
    private readonly db_9f8bee_konxdevContext context;
    private readonly NavigationManager navigationManager;

    public PermitIntelService(db_9f8bee_konxdevContext context, NavigationManager navigationManager)
    {
        this.context = context;
        this.navigationManager = navigationManager;
    }

    // ── Permit Search ──

    public async Task<(IEnumerable<DobjobFiling> Results, int TotalCount)> SearchPermits(PermitSearchQuery query)
    {
        var items = context.DobjobFilings.AsNoTracking().AsQueryable();

        // Text search across address fields
        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            var search = query.SearchText.Trim();
            items = items.Where(i =>
                (i.HouseNum != null && i.HouseNum.Contains(search)) ||
                (i.StreetName != null && i.StreetName.Contains(search)) ||
                (i.JobDescription != null && i.JobDescription.Contains(search)) ||
                (i.Block != null && i.Block.Contains(search)) ||
                (i.Lot != null && i.Lot.Contains(search)) ||
                (i.Bin != null && i.Bin.Contains(search)));
        }

        // Borough filter
        if (query.Boroughs?.Any() == true)
        {
            items = items.Where(i => query.Boroughs.Contains(i.Borough));
        }

        // Job Type filter
        if (query.JobTypes?.Any() == true)
        {
            items = items.Where(i => query.JobTypes.Contains(i.JobType));
        }

        // Job Status filter
        if (query.JobStatuses?.Any() == true)
        {
            items = items.Where(i => query.JobStatuses.Contains(i.JobStatus));
        }

        // Trade filter — check boolean flag columns
        if (query.Trades?.Any() == true)
        {
            foreach (var trade in query.Trades)
            {
                items = trade switch
                {
                    "Plumbing" => items.Where(i => i.Plumbing == "X"),
                    "Mechanical" => items.Where(i => i.Mechanical == "X"),
                    "Boiler" => items.Where(i => i.Boiler == "X"),
                    "FuelBurning" => items.Where(i => i.FuelBurning == "X"),
                    "FuelStorage" => items.Where(i => i.FuelStorage == "X"),
                    "Standpipe" => items.Where(i => i.Standpipe == "X"),
                    "Sprinkler" => items.Where(i => i.Sprinkler == "X"),
                    "FireAlarm" => items.Where(i => i.FireAlarm == "X"),
                    "Equipment" => items.Where(i => i.Equipment == "X"),
                    "FireSuppression" => items.Where(i => i.FireSuppression == "X"),
                    "CurbCut" => items.Where(i => i.CurbCut == "X"),
                    _ => items
                };
            }
        }

        // Cost range filter
        if (query.MinCost.HasValue)
        {
            items = items.Where(i => i.InitialCost >= query.MinCost.Value);
        }
        if (query.MaxCost.HasValue)
        {
            items = items.Where(i => i.InitialCost <= query.MaxCost.Value);
        }

        // Date range filter
        if (query.DateFrom.HasValue)
        {
            items = items.Where(i => i.LatestActionDate >= query.DateFrom.Value);
        }
        if (query.DateTo.HasValue)
        {
            items = items.Where(i => i.LatestActionDate <= query.DateTo.Value);
        }

        // Building type filter
        if (!string.IsNullOrWhiteSpace(query.BuildingType))
        {
            items = items.Where(i => i.BuildingType == query.BuildingType);
        }

        // Get total count before pagination
        var totalCount = await items.CountAsync();

        // Apply ordering
        if (!string.IsNullOrWhiteSpace(query.OrderBy))
        {
            items = items.OrderBy(query.OrderBy);
        }

        // Apply pagination
        var results = await items
            .Skip(query.Skip)
            .Take(query.Take)
            .ToListAsync();

        return (results, totalCount);
    }

    public async Task<DobjobFiling> GetPermitById(int id)
    {
        return await context.DobjobFilings
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    // ── Saved Leads ──

    public async Task<IEnumerable<SavedLead>> GetSavedLeads(string userId)
    {
        return await context.SavedLeads
            .Include(s => s.DobjobFiling)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SavedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<SavedLead> SaveLead(string userId, int dobjobFilingId)
    {
        var existing = await context.SavedLeads
            .FirstOrDefaultAsync(s => s.UserId == userId && s.DobjobFilingId == dobjobFilingId);

        if (existing != null)
            return existing;

        var lead = new SavedLead
        {
            UserId = userId,
            DobjobFilingId = dobjobFilingId,
            Status = "New",
            SavedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.SavedLeads.Add(lead);
        await context.SaveChangesAsync();
        return lead;
    }

    public async Task UpdateLeadStatus(int leadId, string status, string notes = null)
    {
        var lead = await context.SavedLeads.FindAsync(leadId);
        if (lead == null) return;

        lead.Status = status;
        lead.UpdatedAt = DateTime.UtcNow;
        if (notes != null)
            lead.Notes = notes;

        await context.SaveChangesAsync();
    }

    public async Task DeleteLead(int leadId)
    {
        var lead = await context.SavedLeads.FindAsync(leadId);
        if (lead == null) return;

        context.SavedLeads.Remove(lead);
        await context.SaveChangesAsync();
    }

    public async Task<int> GetSavedLeadCount(string userId)
    {
        return await context.SavedLeads.CountAsync(s => s.UserId == userId);
    }

    // ── Alert Preferences ──

    public async Task<AlertPreference> GetAlertPreference(string userId)
    {
        return await context.AlertPreferences
            .FirstOrDefaultAsync(a => a.UserId == userId);
    }

    public async Task SaveAlertPreference(AlertPreference preference)
    {
        var existing = await context.AlertPreferences
            .FirstOrDefaultAsync(a => a.UserId == preference.UserId);

        if (existing != null)
        {
            existing.Boroughs = preference.Boroughs;
            existing.JobTypes = preference.JobTypes;
            existing.Trades = preference.Trades;
            existing.MinCost = preference.MinCost;
            existing.MaxCost = preference.MaxCost;
            existing.AlertChannel = preference.AlertChannel;
            existing.AlertFrequency = preference.AlertFrequency;
            existing.IsActive = preference.IsActive;
        }
        else
        {
            preference.CreatedAt = DateTime.UtcNow;
            context.AlertPreferences.Add(preference);
        }

        await context.SaveChangesAsync();
    }

    // ── Subscriptions ──

    public async Task<Subscription> GetActiveSubscription(string userId)
    {
        return await context.Subscriptions
            .Where(s => s.UserId == userId && (s.Status == "Active" || s.Status == "Trialing"))
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<Subscription> CreateSubscription(Subscription subscription)
    {
        subscription.CreatedAt = DateTime.UtcNow;
        subscription.UpdatedAt = DateTime.UtcNow;
        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync();
        return subscription;
    }

    public async Task UpdateSubscription(int subscriptionId, string status, string tier = null)
    {
        var sub = await context.Subscriptions.FindAsync(subscriptionId);
        if (sub == null) return;

        sub.Status = status;
        sub.UpdatedAt = DateTime.UtcNow;
        if (tier != null)
            sub.Tier = tier;
        if (status == "Canceled")
            sub.EndDate = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    // ── Ingestion Logs ──

    public async Task<IngestionLog> GetLastSuccessfulIngestion()
    {
        return await context.IngestionLogs
            .Where(l => l.Status == "Success")
            .OrderByDescending(l => l.RunDate)
            .FirstOrDefaultAsync();
    }

    public async Task<IngestionLog> LogIngestion(IngestionLog log)
    {
        context.IngestionLogs.Add(log);
        await context.SaveChangesAsync();
        return log;
    }

    // ── Dashboard Stats ──

    public async Task<PermitDashboardStats> GetDashboardStats(string userId)
    {
        var today = DateTime.UtcNow.Date;
        var thirtyDaysAgo = today.AddDays(-30);

        var totalPermits = await context.DobjobFilings
            .CountAsync(p => p.LatestActionDate >= thirtyDaysAgo);

        var savedLeads = await context.SavedLeads
            .CountAsync(s => s.UserId == userId);

        var wonLeads = await context.SavedLeads
            .CountAsync(s => s.UserId == userId && s.Status == "Won");

        var permitsByBorough = await context.DobjobFilings
            .Where(p => p.LatestActionDate >= thirtyDaysAgo && p.Borough != null)
            .GroupBy(p => p.Borough)
            .Select(g => new { Borough = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Borough, g => g.Count);

        return new PermitDashboardStats
        {
            TotalPermitsLast30Days = totalPermits,
            SavedLeadsCount = savedLeads,
            WonLeadsCount = wonLeads,
            PermitsByBorough = permitsByBorough
        };
    }

    // ── Lead Scoring ──

    public static int ScorePermit(DobjobFiling filing)
    {
        int score = 0;

        // +1 for cost > $10K
        if (filing.InitialCost.HasValue && filing.InitialCost.Value > 10_000m)
            score++;

        // +1 for cost > $50K
        if (filing.InitialCost.HasValue && filing.InitialCost.Value > 50_000m)
            score++;

        // +1 for major alteration or new building
        if (filing.JobType is "A1" or "NB")
            score++;

        // +1 for multiple trade flags
        var tradeCount = 0;
        if (filing.Plumbing == "X") tradeCount++;
        if (filing.Mechanical == "X") tradeCount++;
        if (filing.Boiler == "X") tradeCount++;
        if (filing.Sprinkler == "X") tradeCount++;
        if (filing.FireAlarm == "X") tradeCount++;
        if (filing.FireSuppression == "X") tradeCount++;
        if (filing.Equipment == "X") tradeCount++;
        if (filing.Standpipe == "X") tradeCount++;
        if (tradeCount >= 2) score++;

        // +1 for expansion (more units proposed than existing)
        if (int.TryParse(filing.ProposedDwellingUnits, out var proposed) &&
            int.TryParse(filing.ExistingDwellingUnits, out var existing) &&
            proposed > existing)
            score++;

        return Math.Clamp(score, 1, 5);
    }
}

public class PermitDashboardStats
{
    public int TotalPermitsLast30Days { get; set; }
    public int SavedLeadsCount { get; set; }
    public int WonLeadsCount { get; set; }
    public Dictionary<string, int> PermitsByBorough { get; set; } = new();
}
