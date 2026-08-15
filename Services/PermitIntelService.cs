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

        foreach (var r in results)
        {
            var bbl = GetBblFromFiling(r);
            var velocity = await Get311ComplaintVelocity(bbl);
            var (dobCount, hpdCount, hpdClassCCount) = await GetViolationSummaryByBin(r.Bin);
            var breakdown = ScorePermitDetailed(r, velocity, dobCount, hpdClassCCount);
            r.LeadScoreBreakdown = breakdown;
            r.LeadScore = breakdown.TotalScore;
        }

        return (results, totalCount);
    }

    public async Task<DobjobFiling> GetPermitById(int id)
    {
        var filing = await context.DobjobFilings
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);

        if (filing != null)
        {
            var bbl = GetBblFromFiling(filing);
            var velocity = await Get311ComplaintVelocity(bbl);
            var (dobCount, hpdCount, hpdClassCCount) = await GetViolationSummaryByBin(filing.Bin);
            var breakdown = ScorePermitDetailed(filing, velocity, dobCount, hpdClassCCount);
            filing.LeadScoreBreakdown = breakdown;
            filing.LeadScore = breakdown.TotalScore;
        }

        return filing;
    }

    public async Task<IEnumerable<DobViolation>> GetDobViolationsByBin(string bin)
    {
        if (string.IsNullOrWhiteSpace(bin)) return Enumerable.Empty<DobViolation>();
        var cleanBin = bin.Trim();
        return await context.DobViolations
            .Where(v => v.Bin.Trim() == cleanBin || v.Bin == cleanBin)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<HpdViolation>> GetHpdViolationsByBin(string bin)
    {
        if (string.IsNullOrWhiteSpace(bin)) return Enumerable.Empty<HpdViolation>();
        var cleanBin = bin.Trim();
        return await context.HpdViolations
            .Where(v => v.Bin.Trim() == cleanBin || v.Bin == cleanBin)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<(int DobCount, int HpdCount, int HpdClassCCount)> GetViolationSummaryByBin(string bin)
    {
        if (string.IsNullOrWhiteSpace(bin)) return (0, 0, 0);
        var cleanBin = bin.Trim();

        var dobCount = await context.DobViolations
            .CountAsync(v => v.Bin.Trim() == cleanBin || v.Bin == cleanBin);

        var hpdViolations = await context.HpdViolations
            .Where(v => v.Bin.Trim() == cleanBin || v.Bin == cleanBin)
            .Select(v => v.Class)
            .ToListAsync();

        var hpdCount = hpdViolations.Count;
        var hpdClassCCount = hpdViolations.Count(c => c == "C");

        return (dobCount, hpdCount, hpdClassCCount);
    }

    // ── Saved Leads ──

    public async Task<IEnumerable<SavedLead>> GetSavedLeads(string userId)
    {
        var saved = await context.SavedLeads
            .Include(s => s.DobjobFiling)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SavedAt)
            .AsNoTracking()
            .ToListAsync();

        foreach (var lead in saved)
        {
            if (lead.DobjobFiling != null)
            {
                var bbl = GetBblFromFiling(lead.DobjobFiling);
                var velocity = await Get311ComplaintVelocity(bbl);
                var (dobCount, hpdCount, hpdClassCCount) = await GetViolationSummaryByBin(lead.DobjobFiling.Bin);
                var breakdown = ScorePermitDetailed(lead.DobjobFiling, velocity, dobCount, hpdClassCCount);
                lead.DobjobFiling.LeadScoreBreakdown = breakdown;
                lead.DobjobFiling.LeadScore = breakdown.TotalScore;
            }
        }

        return saved;
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

    public async Task BulkUpdateLeadStatus(List<int> leadIds, string newStatus)
    {
        if (leadIds == null || !leadIds.Any()) return;

        var leads = await context.SavedLeads
            .Where(s => leadIds.Contains(s.Id))
            .ToListAsync();

        foreach (var lead in leads)
        {
            lead.Status = newStatus;
            lead.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
    }

    public static byte[] ExportLeadsToCsv(IEnumerable<SavedLead> leads)
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("Lead ID,Permit ID,Job Num,House Num,Street Name,Borough,Job Type,Est Cost,Lead Score,Lead Tier,Status,Notes,Saved At");

        foreach (var lead in leads ?? Enumerable.Empty<SavedLead>())
        {
            var f = lead.DobjobFiling;
            var jobNum = EscapeCsv(f?.JobNum?.ToString());
            var houseNum = EscapeCsv(f?.HouseNum);
            var streetName = EscapeCsv(f?.StreetName);
            var borough = EscapeCsv(f?.Borough);
            var jobType = EscapeCsv(f?.JobType);
            var estCost = f?.InitialCost?.ToString("C0") ?? "N/A";
            var score = f?.LeadScore ?? 1;
            var tier = f?.LeadScoreBreakdown?.Tier ?? (score >= 4 ? "Hot" : "Standard");
            var status = EscapeCsv(lead.Status);
            var notes = EscapeCsv(lead.Notes);
            var savedAt = lead.SavedAt.ToString("yyyy-MM-dd HH:mm:ss");

            builder.AppendLine($"{lead.Id},{lead.DobjobFilingId},{jobNum},{houseNum},{streetName},{borough},{jobType},\"{estCost}\",{score},{tier},{status},{notes},{savedAt}");
        }

        return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static string EscapeCsv(string val)
    {
        if (string.IsNullOrEmpty(val)) return "";
        return $"\"{val.Replace("\"", "\"\"")}\"";
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

    public static string GetBblFromFiling(DobjobFiling filing)
    {
        if (string.IsNullOrWhiteSpace(filing.Borough) || string.IsNullOrWhiteSpace(filing.Block) || string.IsNullOrWhiteSpace(filing.Lot))
            return null;

        int boroDigit = filing.Borough.ToUpper() switch
        {
            "MANHATTAN" => 1,
            "BRONX" => 2,
            "BROOKLYN" => 3,
            "QUEENS" => 4,
            "STATEN ISLAND" => 5,
            _ => 0
        };

        if (boroDigit == 0) return null;

        // Block is 5 digits, Lot is 4 digits
        return $"{boroDigit}{filing.Block.PadLeft(5, '0')}{filing.Lot.PadLeft(4, '0')}";
    }

    public async Task<int> Get311ComplaintVelocity(string bbl, int days = 90)
    {
        if (string.IsNullOrWhiteSpace(bbl)) return 0;
        
        var cutoff = DateTime.UtcNow.AddDays(-days);
        return await context.ServiceRequests311
            .CountAsync(s => s.Bbl == bbl && s.CreatedDate >= cutoff);
    }

    public async Task<List<ServiceRequest311>> Get311ComplaintsByBbl(string bbl)
    {
        if (string.IsNullOrWhiteSpace(bbl)) return new List<ServiceRequest311>();

        return await context.ServiceRequests311
            .Where(s => s.Bbl == bbl)
            .OrderByDescending(s => s.CreatedDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public static LeadScoreBreakdown ScorePermitDetailed(DobjobFiling filing, int complaintVelocity = 0, int activeDobViolations = 0, int hpdClassCCount = 0)
    {
        var breakdown = new LeadScoreBreakdown();
        int rawScore = 0;

        // +1 for cost > $10K, +1 for cost > $50K
        if (filing.InitialCost.HasValue && filing.InitialCost.Value > 50_000m)
        {
            rawScore += 2;
            breakdown.CostPoints = 2;
            breakdown.Factors.Add(new LeadScoreFactor
            {
                Name = "High Estimated Job Cost",
                Points = 2,
                Description = $"Estimated job cost is {filing.InitialCost.Value:C0} (>$50,000)",
                Category = "Cost",
                BadgeStyle = "success"
            });
        }
        else if (filing.InitialCost.HasValue && filing.InitialCost.Value > 10_000m)
        {
            rawScore += 1;
            breakdown.CostPoints = 1;
            breakdown.Factors.Add(new LeadScoreFactor
            {
                Name = "Moderate Estimated Job Cost",
                Points = 1,
                Description = $"Estimated job cost is {filing.InitialCost.Value:C0} (>$10,000)",
                Category = "Cost",
                BadgeStyle = "info"
            });
        }

        // +1 for major alteration or new building
        if (filing.JobType is "A1" or "NB")
        {
            rawScore += 1;
            breakdown.JobTypePoints = 1;
            breakdown.Factors.Add(new LeadScoreFactor
            {
                Name = filing.JobType == "NB" ? "New Building Construction" : "Major Alteration (A1)",
                Points = 1,
                Description = $"Filing category '{filing.JobType}' indicates major scope of work",
                Category = "JobType",
                BadgeStyle = "warning"
            });
        }

        // +1 for multiple trade flags
        var tradeCount = 0;
        var tradesList = new List<string>();
        if (filing.Plumbing == "X") { tradeCount++; tradesList.Add("Plumbing"); }
        if (filing.Mechanical == "X") { tradeCount++; tradesList.Add("Mechanical"); }
        if (filing.Boiler == "X") { tradeCount++; tradesList.Add("Boiler"); }
        if (filing.Sprinkler == "X") { tradeCount++; tradesList.Add("Sprinkler"); }
        if (filing.FireAlarm == "X") { tradeCount++; tradesList.Add("Fire Alarm"); }
        if (filing.FireSuppression == "X") { tradeCount++; tradesList.Add("Fire Suppression"); }
        if (filing.Equipment == "X") { tradeCount++; tradesList.Add("Equipment"); }
        if (filing.Standpipe == "X") { tradeCount++; tradesList.Add("Standpipe"); }

        if (tradeCount >= 2)
        {
            rawScore += 1;
            breakdown.TradePoints = 1;
            breakdown.Factors.Add(new LeadScoreFactor
            {
                Name = "Multi-Trade Scope",
                Points = 1,
                Description = $"Includes {tradeCount} trade disciplines ({string.Join(", ", tradesList)})",
                Category = "Trade",
                BadgeStyle = "info"
            });
        }

        // Predictive Intel Boost (311 Complaints)
        if (complaintVelocity >= 3)
        {
            rawScore += 2;
            breakdown.ComplaintPoints = 2;
            breakdown.Factors.Add(new LeadScoreFactor
            {
                Name = "High 311 Complaint Velocity",
                Points = 2,
                Description = $"{complaintVelocity} complaints recorded at this BBL in past 90 days",
                Category = "Complaint",
                BadgeStyle = "danger"
            });
        }
        else if (complaintVelocity > 0)
        {
            rawScore += 1;
            breakdown.ComplaintPoints = 1;
            breakdown.Factors.Add(new LeadScoreFactor
            {
                Name = "Active 311 Complaints",
                Points = 1,
                Description = $"{complaintVelocity} complaint(s) recorded at this BBL in past 90 days",
                Category = "Complaint",
                BadgeStyle = "warning"
            });
        }

        // Violation Boosts:
        if (activeDobViolations >= 3)
        {
            rawScore += 2;
            breakdown.DobViolationPoints = 2;
            breakdown.Factors.Add(new LeadScoreFactor
            {
                Name = "Multiple Open DOB Violations",
                Points = 2,
                Description = $"{activeDobViolations} open DOB violations at BIN {filing.Bin}",
                Category = "Violation",
                BadgeStyle = "danger"
            });
        }
        else if (activeDobViolations > 0)
        {
            rawScore += 1;
            breakdown.DobViolationPoints = 1;
            breakdown.Factors.Add(new LeadScoreFactor
            {
                Name = "Open DOB Violation",
                Points = 1,
                Description = $"{activeDobViolations} open DOB violation(s) at BIN {filing.Bin}",
                Category = "Violation",
                BadgeStyle = "warning"
            });
        }

        // Severe HPD Class C Boost
        if (hpdClassCCount > 0)
        {
            rawScore += 2;
            breakdown.HpdViolationPoints = 2;
            breakdown.Factors.Add(new LeadScoreFactor
            {
                Name = "Class C HPD Violation (Hazardous)",
                Points = 2,
                Description = $"{hpdClassCCount} immediately hazardous Class C HPD violation(s)",
                Category = "Violation",
                BadgeStyle = "danger"
            });
        }

        // Expansion Boost
        if (int.TryParse(filing.ProposedDwellingUnits, out var proposed) &&
            int.TryParse(filing.ExistingDwellingUnits, out var existing) &&
            proposed > existing)
        {
            rawScore += 1;
            breakdown.ExpansionPoints = 1;
            breakdown.Factors.Add(new LeadScoreFactor
            {
                Name = "Unit Expansion Project",
                Points = 1,
                Description = $"Dwelling units increasing from {existing} to {proposed}",
                Category = "Expansion",
                BadgeStyle = "success"
            });
        }

        breakdown.RawScore = rawScore;
        var totalScore = Math.Clamp(rawScore, 1, 5);
        breakdown.TotalScore = totalScore;

        breakdown.Tier = totalScore switch
        {
            5 => "Hot",
            4 => "High Priority",
            3 => "Medium Priority",
            _ => "Standard"
        };

        return breakdown;
    }

    public static int ScorePermit(DobjobFiling filing, int complaintVelocity = 0, int activeDobViolations = 0, int hpdClassCCount = 0)
    {
        return ScorePermitDetailed(filing, complaintVelocity, activeDobViolations, hpdClassCCount).TotalScore;
    }
}

public class PermitDashboardStats
{
    public int TotalPermitsLast30Days { get; set; }
    public int SavedLeadsCount { get; set; }
    public int WonLeadsCount { get; set; }
    public Dictionary<string, int> PermitsByBorough { get; set; } = new();
}
