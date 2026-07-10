using KonXProWebApp.Data;
using KonXProWebApp.Models.db_9f8bee_konxdev;
using KonXProWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KonXProWebApp.Controllers;

/// <summary>
/// REST API for permit data. Requires Agency tier subscription.
/// </summary>
[ApiController]
[Route("api/permit-intel")]
[Authorize(Policy = "RequiresAgency")]
public class PermitIntelApiController : ControllerBase
{
    private readonly db_9f8bee_konxdevContext _context;
    private readonly ILogger<PermitIntelApiController> _logger;

    public PermitIntelApiController(
        db_9f8bee_konxdevContext context,
        ILogger<PermitIntelApiController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Search permits with filters.
    /// GET /api/permit-intel/permits?borough=BROOKLYN&amp;jobType=NB&amp;minCost=50000&amp;take=25&amp;skip=0
    /// </summary>
    [HttpGet("permits")]
    public async Task<IActionResult> SearchPermits(
        [FromQuery] string borough = null,
        [FromQuery] string jobType = null,
        [FromQuery] string search = null,
        [FromQuery] decimal? minCost = null,
        [FromQuery] decimal? maxCost = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 25)
    {
        take = Math.Clamp(take, 1, 100);

        var query = _context.DobjobFilings.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(borough))
            query = query.Where(p => p.Borough == borough);

        if (!string.IsNullOrWhiteSpace(jobType))
            query = query.Where(p => p.JobType == jobType);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(p =>
                (p.StreetName != null && p.StreetName.Contains(s)) ||
                (p.JobDescription != null && p.JobDescription.Contains(s)));
        }

        if (minCost.HasValue)
            query = query.Where(p => p.InitialCost >= minCost.Value);

        if (maxCost.HasValue)
            query = query.Where(p => p.InitialCost <= maxCost.Value);

        if (dateFrom.HasValue)
            query = query.Where(p => p.LatestActionDate >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(p => p.LatestActionDate <= dateTo.Value);

        var totalCount = await query.CountAsync();

        var results = await query
            .OrderByDescending(p => p.LatestActionDate)
            .Skip(skip)
            .Take(take)
            .Select(p => new
            {
                p.Id,
                p.JobNum,
                p.DocNum,
                p.Borough,
                Address = (p.HouseNum ?? "") + " " + (p.StreetName ?? ""),
                p.JobType,
                p.JobStatus,
                p.JobStatusDescrp,
                p.InitialCost,
                p.LatestActionDate,
                p.BuildingType,
                p.JobDescription,
                p.LeadScore,
                Location = new
                {
                    Lat = p.Gislatitude,
                    Lng = p.Gislongitude
                }
            })
            .ToListAsync();

        return Ok(new
        {
            totalCount,
            skip,
            take,
            results
        });
    }

    /// <summary>
    /// Get a single permit by ID.
    /// GET /api/permit-intel/permits/{id}
    /// </summary>
    [HttpGet("permits/{id:int}")]
    public async Task<IActionResult> GetPermit(int id)
    {
        var permit = await _context.DobjobFilings
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (permit == null)
            return NotFound(new { error = "Permit not found" });

        return Ok(permit);
    }

    /// <summary>
    /// Get borough summary statistics.
    /// GET /api/permit-intel/stats/boroughs
    /// </summary>
    [HttpGet("stats/boroughs")]
    public async Task<IActionResult> GetBoroughStats(
        [FromQuery] int days = 30)
    {
        days = Math.Clamp(days, 1, 365);
        var since = DateTime.UtcNow.AddDays(-days);

        var stats = await _context.DobjobFilings
            .Where(p => p.LatestActionDate >= since && p.Borough != null)
            .GroupBy(p => p.Borough)
            .Select(g => new { Borough = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        return Ok(stats);
    }
}
